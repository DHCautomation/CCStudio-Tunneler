using CCStudio.Tunneler.Core.Interfaces;
using CCStudio.Tunneler.Core.Models;
using Newtonsoft.Json;
using Serilog;

namespace CCStudio.Tunneler.Core.Services;

/// <summary>
/// Service for managing configuration
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private const string DefaultConfigPath = @"C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json";
    private TunnelerConfiguration _currentConfiguration;
    private readonly ILogger _logger;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public ConfigurationService(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
        _currentConfiguration = GetDefaultConfiguration();
    }

    /// <summary>
    /// Load configuration from file
    /// </summary>
    public async Task<TunnelerConfiguration> LoadConfigurationAsync(string? filePath = null)
    {
        var path = filePath ?? DefaultConfigPath;

        try
        {
            if (!File.Exists(path))
            {
                _logger.Warning("Configuration file not found at {Path}, creating default configuration", path);
                var defaultConfig = GetDefaultConfiguration();
                await SaveConfigurationAsync(defaultConfig, path);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(path);
            var config = JsonConvert.DeserializeObject<TunnelerConfiguration>(json);

            if (config == null)
            {
                _logger.Error("Failed to deserialize configuration, using default");
                return GetDefaultConfiguration();
            }

            var (isValid, errors) = await ValidateConfigurationAsync(config);
            if (!isValid)
            {
                _logger.Error("Configuration validation failed: {Errors}", string.Join(", ", errors));
                throw new InvalidOperationException($"Invalid configuration: {string.Join(", ", errors)}");
            }

            var oldConfig = _currentConfiguration;
            _currentConfiguration = config;

            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                OldConfiguration = oldConfig,
                NewConfiguration = config
            });

            _logger.Information("Configuration loaded successfully from {Path}", path);
            return config;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading configuration from {Path}", path);
            throw;
        }
    }

    /// <summary>
    /// Save configuration to file
    /// </summary>
    public async Task<bool> SaveConfigurationAsync(TunnelerConfiguration configuration, string? filePath = null)
    {
        var path = filePath ?? DefaultConfigPath;

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(configuration, Formatting.Indented);
            await File.WriteAllTextAsync(path, json);

            _logger.Information("Configuration saved successfully to {Path}", path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving configuration to {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Get the current configuration
    /// </summary>
    public TunnelerConfiguration GetConfiguration()
    {
        return _currentConfiguration;
    }

    /// <summary>
    /// Validate configuration
    /// </summary>
    public Task<(bool isValid, List<string> errors)> ValidateConfigurationAsync(TunnelerConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate OPC DA configuration
        if (string.IsNullOrWhiteSpace(configuration.OpcDa.ServerProgId))
        {
            errors.Add("OPC DA Server ProgID is required");
        }

        if (configuration.OpcDa.UpdateRate < 100 || configuration.OpcDa.UpdateRate > 60000)
        {
            errors.Add("OPC DA Update Rate must be between 100ms and 60000ms");
        }

        if (configuration.OpcDa.DeadBand < 0 || configuration.OpcDa.DeadBand > 100)
        {
            errors.Add("OPC DA Dead Band must be between 0 and 100");
        }

        // Validate OPC UA configuration
        if (configuration.OpcUa.ServerPort < 1024 || configuration.OpcUa.ServerPort > 65535)
        {
            errors.Add("OPC UA Server Port must be between 1024 and 65535");
        }

        if (string.IsNullOrWhiteSpace(configuration.OpcUa.ServerName))
        {
            errors.Add("OPC UA Server Name is required");
        }

        if (string.IsNullOrWhiteSpace(configuration.OpcUa.EndpointUrl))
        {
            errors.Add("OPC UA Endpoint URL is required");
        }

        if (!configuration.OpcUa.AllowAnonymous)
        {
            if (string.IsNullOrWhiteSpace(configuration.OpcUa.Username))
            {
                errors.Add("OPC UA Username is required when anonymous access is disabled");
            }

            if (string.IsNullOrWhiteSpace(configuration.OpcUa.Password))
            {
                errors.Add("OPC UA Password is required when anonymous access is disabled");
            }
        }

        // Validate logging configuration
        if (string.IsNullOrWhiteSpace(configuration.Logging.Path))
        {
            errors.Add("Logging path is required");
        }

        if (configuration.Logging.RetentionDays < 1)
        {
            errors.Add("Log retention days must be at least 1");
        }

        if (configuration.Logging.MaxFileSizeMB < 1)
        {
            errors.Add("Maximum log file size must be at least 1 MB");
        }

        // Validate tag mappings
        var duplicateMappings = configuration.TagMappings
            .GroupBy(m => m.DaTagName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicate in duplicateMappings)
        {
            errors.Add($"Duplicate OPC DA tag mapping: {duplicate}");
        }

        return Task.FromResult((errors.Count == 0, errors));
    }

    /// <summary>
    /// Export configuration to JSON string
    /// </summary>
    public string ExportConfiguration(TunnelerConfiguration configuration)
    {
        return JsonConvert.SerializeObject(configuration, Formatting.Indented);
    }

    /// <summary>
    /// Import configuration from JSON string
    /// </summary>
    public async Task<TunnelerConfiguration?> ImportConfigurationAsync(string json)
    {
        try
        {
            var config = JsonConvert.DeserializeObject<TunnelerConfiguration>(json);

            if (config == null)
            {
                _logger.Error("Failed to deserialize configuration JSON");
                return null;
            }

            var (isValid, errors) = await ValidateConfigurationAsync(config);
            if (!isValid)
            {
                _logger.Error("Imported configuration validation failed: {Errors}", string.Join(", ", errors));
                return null;
            }

            return config;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error importing configuration from JSON");
            return null;
        }
    }

    /// <summary>
    /// Get default configuration
    /// </summary>
    public TunnelerConfiguration GetDefaultConfiguration()
    {
        return new TunnelerConfiguration
        {
            OpcDa = new OpcDaConfiguration
            {
                ServerProgId = "",
                ServerHost = "localhost",
                UpdateRate = 1000,
                DeadBand = 0.0f,
                Tags = new List<string> { "*" },
                AutoDiscoverTags = true,
                ConnectionTimeout = 30,
                MaxReconnectAttempts = 0,
                ReconnectDelay = 5
            },
            OpcUa = new OpcUaConfiguration
            {
                ServerPort = 4840,
                ServerName = "CCStudio Tunneler",
                EndpointUrl = "opc.tcp://localhost:4840",
                SecurityMode = SecurityMode.None,
                AllowAnonymous = true,
                ApplicationName = "CCStudio-Tunneler",
                ApplicationUri = "urn:DHCAutomation:CCStudio-Tunneler",
                ProductUri = "https://dhcautomation.com/ccstudio-tunneler",
                MaxSessionCount = 100,
                MaxSubscriptionCount = 100,
                MinPublishingInterval = 100,
                MaxPublishingInterval = 10000
            },
            Logging = new LoggingConfiguration
            {
                Level = "Information",
                Path = @"C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs",
                RetentionDays = 7,
                MaxFileSizeMB = 10,
                EnableConsole = true
            },
            TagMappings = new List<TagMapping>(),
            EnableMetrics = true,
            ServiceName = "CCStudio-Tunneler",
            ServiceDescription = "OPC DA to OPC UA Bridge by DHC Automation and Controls"
        };
    }
}
