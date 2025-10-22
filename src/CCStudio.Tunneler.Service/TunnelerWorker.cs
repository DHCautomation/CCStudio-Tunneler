using CCStudio.Tunneler.Core.Interfaces;
using CCStudio.Tunneler.Core.Models;
using CCStudio.Tunneler.Core.Services;
using CCStudio.Tunneler.Core.Utilities;
using CCStudio.Tunneler.Service.OPC;
using CCStudio.Tunneler.Service.Utilities;

namespace CCStudio.Tunneler.Service;

/// <summary>
/// Represents a connected OPC DA server
/// </summary>
internal class DaServerConnection
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string UaNamespace { get; set; } = string.Empty;
    public IOpcDaClient Client { get; set; } = null!;
    public ReconnectionManager ReconnectionManager { get; set; } = null!;
    public ConnectionStatus Status { get; set; } = ConnectionStatus.Disconnected;
    public DateTime? LastConnected { get; set; }
    public int SubscribedTagCount { get; set; }
}

/// <summary>
/// Main worker service that coordinates OPC DA clients and OPC UA server
/// Now supports multiple OPC DA servers simultaneously
/// </summary>
public class TunnelerWorker : BackgroundService
{
    private readonly ILogger<TunnelerWorker> _logger;
    private readonly ConfigurationService _configService;
    private TunnelerConfiguration? _configuration;
    private TunnelerStatus _status;
    private DateTime _startTime;
    private long _messagesProcessed;
    private System.Diagnostics.Stopwatch _uptimeWatch;

    // Multi-server support
    private readonly Dictionary<string, DaServerConnection> _daServers = new();
    private IOpcUaServer? _uaServer;

    public TunnelerWorker(ILogger<TunnelerWorker> logger, ConfigurationService configService)
    {
        _logger = logger;
        _configService = configService;
        _status = new TunnelerStatus();
        _uptimeWatch = new System.Diagnostics.Stopwatch();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CCStudio-Tunneler Worker starting at: {Time}", DateTimeOffset.Now);
        _logger.LogInformation("Version: 1.1.0 - Multi-Server Support");
        _startTime = DateTime.UtcNow;
        _uptimeWatch.Start();

        try
        {
            // Load configuration
            _logger.LogInformation("Loading configuration...");
            _configuration = await _configService.LoadConfigurationAsync();
            _status.ConfigurationLoaded = true;

            // Validate configuration
            ValidateConfiguration(_configuration);

            // Reconfigure logger with loaded settings
            if (_configuration?.Logging != null)
            {
                LoggerFactory.EnsureLogDirectoryExists(_configuration.Logging.Path);
                _logger.LogInformation("Logging configured: Level={Level}, Path={Path}",
                    _configuration.Logging.Level, _configuration.Logging.Path);
            }

            _status.State = ServiceState.Starting;

            // Initialize OPC UA Server first
            _logger.LogInformation("========================================");
            _logger.LogInformation("Starting OPC UA Server...");
            _logger.LogInformation("OPC UA Endpoint: {EndpointUrl}", _configuration.OpcUa.EndpointUrl);

            _uaServer = new OpcUaServer(_logger);
            _uaServer.NodeWritten += OnUaNodeWritten;
            _uaServer.ClientConnected += OnClientConnected;
            _uaServer.ClientDisconnected += OnClientDisconnected;

            if (await _uaServer.StartAsync(_configuration.OpcUa))
            {
                _status.UaServerStatus = ConnectionStatus.Connected;
                _logger.LogInformation("✓ OPC UA Server started successfully");
            }
            else
            {
                _status.UaServerStatus = ConnectionStatus.Error;
                _logger.LogError("✗ Failed to start OPC UA Server");
                throw new Exception("OPC UA Server failed to start");
            }

            // Initialize OPC DA Clients (multi-server support)
            _logger.LogInformation("========================================");
            _logger.LogInformation("Initializing OPC DA connections...");

            var daSourcesList = GetDaSourcesList(_configuration);
            _logger.LogInformation("Found {Count} OPC DA server(s) to connect", daSourcesList.Count);

            foreach (var source in daSourcesList)
            {
                if (!source.Enabled)
                {
                    _logger.LogInformation("Skipping disabled server: {Name}", source.Name);
                    continue;
                }

                await InitializeDaServerAsync(source);
            }

            _status.State = ServiceState.Running;
            _status.StartTime = _startTime;

            var connectedCount = _daServers.Values.Count(s => s.Status == ConnectionStatus.Connected);
            _logger.LogInformation("========================================");
            _logger.LogInformation("✓ CCStudio-Tunneler is now running");
            _logger.LogInformation("  - OPC UA Server: {Endpoint}", _configuration.OpcUa.EndpointUrl);
            _logger.LogInformation("  - OPC DA Servers: {Connected}/{Total} connected",
                connectedCount, _daServers.Count);
            _logger.LogInformation("========================================");

            // Main service loop
            await RunServiceLoopAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in worker service");
            _status.State = ServiceState.Error;
            _status.LastError = ex.Message;
            throw;
        }
    }

    private async Task RunServiceLoopAsync(CancellationToken stoppingToken)
    {
        var loopCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                loopCount++;

                // Update status
                _status.Uptime = _uptimeWatch.Elapsed;
                _status.LastUpdate = DateTime.UtcNow;
                _status.TotalMessagesProcessed = _messagesProcessed;
                _status.ConnectedClients = _uaServer?.GetConnectedClientCount() ?? 0;
                _status.ActiveTagCount = _configuration?.TagMappings.Count(m => m.Enabled) ?? 0;

                // Check and reconnect all OPC DA servers
                foreach (var serverEntry in _daServers.Values)
                {
                    await CheckAndReconnectServerAsync(serverEntry);
                }

                // Log periodic heartbeat (every 60 seconds = 6 loops at 10s interval)
                if (loopCount % 6 == 0)
                {
                    var connectedServers = _daServers.Values.Count(s => s.Status == ConnectionStatus.Connected);
                    var totalServers = _daServers.Count;

                    _logger.LogInformation(
                        "Heartbeat - Uptime: {Uptime}, Messages: {Messages}, Clients: {Clients}, " +
                        "Tags: {Tags}, DA Servers: {Connected}/{Total}",
                        _status.Uptime.ToString(@"dd\.hh\:mm\:ss"),
                        _messagesProcessed,
                        _status.ConnectedClients,
                        _status.ActiveTagCount,
                        connectedServers,
                        totalServers);
                }

                // Wait before next iteration
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in service loop");
                _status.ErrorCount++;
                _status.LastError = ex.Message;
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task CheckAndReconnectServerAsync(DaServerConnection server)
    {
        if (server.Client.IsConnected)
        {
            server.Status = ConnectionStatus.Connected;
            return;
        }

        // Check if should attempt reconnection
        if (!server.ReconnectionManager.ShouldAttemptReconnection())
        {
            return;
        }

        // Attempt reconnection
        _logger.LogInformation("Attempting to reconnect to OPC DA Server: {Name}", server.ServerName);
        server.Status = ConnectionStatus.Reconnecting;
        server.ReconnectionManager.RecordAttempt();

        var sourceConfig = GetSourceConfiguration(server.ServerId);
        if (sourceConfig == null)
        {
            _logger.LogError("Configuration not found for server: {ServerId}", server.ServerId);
            return;
        }

        if (await server.Client.ConnectAsync(sourceConfig.Configuration))
        {
            server.Status = ConnectionStatus.Connected;
            server.LastConnected = DateTime.UtcNow;
            server.ReconnectionManager.RecordSuccess();
            _logger.LogInformation("✓ Reconnected to OPC DA Server: {Name}", server.ServerName);

            // Re-subscribe to tags
            await SubscribeToTagsAsync(server);
        }
        else
        {
            server.Status = ConnectionStatus.Error;
            server.ReconnectionManager.RecordFailure($"Failed to connect to {server.ServerName}");
        }
    }

    private async Task InitializeDaServerAsync(OpcDaSource source)
    {
        _logger.LogInformation("Connecting to OPC DA Server: {Name}", source.Name);
        _logger.LogInformation("  ProgID: {ProgId}", source.Configuration.ServerProgId);
        _logger.LogInformation("  Host: {Host}", source.Configuration.ServerHost);
        _logger.LogInformation("  Namespace: {Namespace}", source.UaNamespace);

        var client = new OpcDaClient(_logger);
        var reconnectMgr = new ReconnectionManager(
            _logger,
            source.Name,
            source.Configuration.ReconnectDelay,
            300, // max delay
            source.Configuration.MaxReconnectAttempts);

        var serverConnection = new DaServerConnection
        {
            ServerId = source.Id,
            ServerName = source.Name,
            UaNamespace = source.UaNamespace,
            Client = client,
            ReconnectionManager = reconnectMgr,
            Status = ConnectionStatus.Disconnected
        };

        // Wire up events with server-specific handlers
        client.TagValueChanged += (sender, e) => OnDaTagValueChanged(sender, e, serverConnection);
        client.ConnectionStatusChanged += (sender, e) => OnDaConnectionStatusChanged(sender, e, serverConnection);

        _daServers[source.Id] = serverConnection;

        // Attempt initial connection
        if (await client.ConnectAsync(source.Configuration))
        {
            serverConnection.Status = ConnectionStatus.Connected;
            serverConnection.LastConnected = DateTime.UtcNow;
            reconnectMgr.RecordSuccess();
            _logger.LogInformation("✓ Connected to OPC DA Server: {Name}", source.Name);

            // Subscribe to tags
            await SubscribeToTagsAsync(serverConnection);
        }
        else
        {
            serverConnection.Status = ConnectionStatus.Error;
            _logger.LogWarning("✗ Failed to connect to OPC DA Server: {Name} - will retry later", source.Name);
        }
    }

    private async Task SubscribeToTagsAsync(DaServerConnection server)
    {
        if (_configuration == null || _uaServer == null)
            return;

        try
        {
            // Get mappings for this server
            var serverMappings = _configuration.TagMappings
                .Where(m => m.Enabled && (m.ServerId == server.ServerId || string.IsNullOrEmpty(m.ServerId)))
                .ToList();

            if (serverMappings.Count == 0)
            {
                _logger.LogInformation("No tag mappings configured for server: {Name}", server.ServerName);
                return;
            }

            // Subscribe to tags in OPC DA
            var tagNames = serverMappings.Select(m => m.DaTagName).ToList();
            await server.Client.SubscribeToTagsAsync(tagNames);
            server.SubscribedTagCount = tagNames.Count;

            // Create corresponding nodes in OPC UA (with namespace prefix)
            foreach (var mapping in serverMappings)
            {
                // Apply namespace prefix if not already present
                var uaNodeName = mapping.UaNodeName;
                if (!string.IsNullOrEmpty(server.UaNamespace) && !uaNodeName.StartsWith(server.UaNamespace))
                {
                    uaNodeName = $"{server.UaNamespace}/{mapping.UaNodeName}";
                }

                var mappingWithNamespace = new TagMapping
                {
                    DaTagName = mapping.DaTagName,
                    UaNodeName = uaNodeName,
                    NamespaceIndex = mapping.NamespaceIndex,
                    Enabled = mapping.Enabled,
                    AccessLevel = mapping.AccessLevel,
                    DataType = mapping.DataType,
                    ScaleFactor = mapping.ScaleFactor,
                    Offset = mapping.Offset,
                    Description = mapping.Description,
                    EngineeringUnits = mapping.EngineeringUnits,
                    UpdateRate = mapping.UpdateRate,
                    Deadband = mapping.Deadband,
                    ServerId = server.ServerId,
                    MinValue = mapping.MinValue,
                    MaxValue = mapping.MaxValue
                };

                await _uaServer.AddOrUpdateNodeAsync(mappingWithNamespace);
                _logger.LogDebug("Mapped tag: {DaTag} -> {UaNode}", mapping.DaTagName, uaNodeName);
            }

            _logger.LogInformation("✓ Subscribed to {Count} tags from {Server}",
                serverMappings.Count, server.ServerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to tags for server: {Name}", server.ServerName);
        }
    }

    private void OnDaTagValueChanged(object? sender, TagValueChangedEventArgs e, DaServerConnection server)
    {
        try
        {
            if (_uaServer == null || _configuration == null)
                return;

            // Find the mapping for this tag
            var mapping = _configuration.TagMappings.FirstOrDefault(m =>
                m.DaTagName == e.TagValue.TagName &&
                m.Enabled &&
                (m.ServerId == server.ServerId || string.IsNullOrEmpty(m.ServerId)));

            if (mapping == null)
                return;

            // Apply scaling if configured
            var value = e.TagValue.Value;
            if (value is double or float or int or long && mapping.ScaleFactor.HasValue)
            {
                var numericValue = Convert.ToDouble(value);
                numericValue = (numericValue * mapping.ScaleFactor.Value) + (mapping.Offset ?? 0);
                value = numericValue;
            }

            // Validate range if configured
            if (value is double or float or int or long)
            {
                var numericValue = Convert.ToDouble(value);
                if (mapping.MinValue.HasValue && numericValue < mapping.MinValue.Value)
                {
                    _logger.LogWarning("Tag {Tag} value {Value} below minimum {Min}",
                        e.TagValue.TagName, numericValue, mapping.MinValue.Value);
                }
                if (mapping.MaxValue.HasValue && numericValue > mapping.MaxValue.Value)
                {
                    _logger.LogWarning("Tag {Tag} value {Value} above maximum {Max}",
                        e.TagValue.TagName, numericValue, mapping.MaxValue.Value);
                }
            }

            // Apply namespace prefix
            var uaNodeName = mapping.UaNodeName;
            if (!string.IsNullOrEmpty(server.UaNamespace) && !uaNodeName.StartsWith(server.UaNamespace))
            {
                uaNodeName = $"{server.UaNamespace}/{mapping.UaNodeName}";
            }

            // Create UA value
            var uaValue = new TagValue
            {
                TagName = uaNodeName,
                Value = value,
                Quality = e.TagValue.Quality,
                Timestamp = e.TagValue.Timestamp,
                DataType = e.TagValue.DataType,
                Source = $"DA:{server.ServerId}"
            };

            // Update OPC UA node
            _uaServer.UpdateNodeValueAsync(uaNodeName, uaValue).Wait();

            _messagesProcessed++;

            if (_messagesProcessed % 100 == 0)
            {
                _logger.LogDebug("Bridged DA->UA [{Server}]: {DaTag} = {Value} -> {UaNode}",
                    server.ServerName, e.TagValue.TagName, value, uaNodeName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bridging DA->UA for tag {Tag} from server {Server}",
                e.TagValue.TagName, server.ServerName);
            _status.ErrorCount++;
        }
    }

    private void OnUaNodeWritten(object? sender, NodeWriteEventArgs e)
    {
        try
        {
            if (_configuration == null)
                return;

            // Find the reverse mapping - check all servers
            TagMapping? mapping = null;
            DaServerConnection? targetServer = null;

            foreach (var server in _daServers.Values)
            {
                // Check with namespace prefix
                var nodeNameWithNs = e.NodeName;
                if (!string.IsNullOrEmpty(server.UaNamespace) && e.NodeName.StartsWith(server.UaNamespace + "/"))
                {
                    nodeNameWithNs = e.NodeName.Substring(server.UaNamespace.Length + 1);
                }

                mapping = _configuration.TagMappings.FirstOrDefault(m =>
                    (m.UaNodeName == e.NodeName || m.UaNodeName == nodeNameWithNs) &&
                    m.Enabled &&
                    m.AccessLevel != AccessLevel.Read &&
                    (m.ServerId == server.ServerId || string.IsNullOrEmpty(m.ServerId)));

                if (mapping != null)
                {
                    targetServer = server;
                    break;
                }
            }

            if (mapping == null || targetServer == null)
            {
                _logger.LogWarning("No writable mapping found for UA node: {NodeName}", e.NodeName);
                return;
            }

            // Apply reverse scaling if configured
            var value = e.Value;
            if (value is double or float or int or long &&
                mapping.ScaleFactor.HasValue &&
                mapping.ScaleFactor.Value != 0)
            {
                var numericValue = Convert.ToDouble(value);
                numericValue = (numericValue - (mapping.Offset ?? 0)) / mapping.ScaleFactor.Value;
                value = numericValue;
            }

            // Write to OPC DA
            if (targetServer.Client.WriteTagAsync(mapping.DaTagName, value!).Result)
            {
                _logger.LogInformation("Bridged UA->DA [{Server}]: {UaNode} = {Value} -> {DaTag}",
                    targetServer.ServerName, e.NodeName, value, mapping.DaTagName);
                _messagesProcessed++;
            }
            else
            {
                _logger.LogWarning("Failed to write to DA tag: {DaTag} on server {Server}",
                    mapping.DaTagName, targetServer.ServerName);
                _status.ErrorCount++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bridging UA->DA for node {Node}", e.NodeName);
            _status.ErrorCount++;
        }
    }

    private void OnDaConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e,
        DaServerConnection server)
    {
        server.Status = e.Status;
        _logger.LogInformation("OPC DA connection status changed [{Server}]: {Status} - {Message}",
            server.ServerName, e.Status, e.Message);
    }

    private void OnClientConnected(object? sender, ClientConnectedEventArgs e)
    {
        _logger.LogInformation("OPC UA Client connected: {ClientName} ({ClientId})",
            e.ClientName ?? "Unknown", e.ClientId);
    }

    private void OnClientDisconnected(object? sender, ClientDisconnectedEventArgs e)
    {
        _logger.LogInformation("OPC UA Client disconnected: {ClientId}", e.ClientId);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CCStudio-Tunneler Worker stopping...");
        _status.State = ServiceState.Stopping;

        try
        {
            // Disconnect all OPC DA Clients
            foreach (var server in _daServers.Values)
            {
                _logger.LogInformation("Disconnecting OPC DA Client: {Name}...", server.ServerName);
                await server.Client.DisconnectAsync();
                server.Client.Dispose();
                server.Status = ConnectionStatus.Disconnected;
            }
            _daServers.Clear();

            // Stop OPC UA Server
            if (_uaServer != null)
            {
                _logger.LogInformation("Stopping OPC UA Server...");
                await _uaServer.StopAsync();
                _uaServer.Dispose();
                _uaServer = null;
            }
            _status.UaServerStatus = ConnectionStatus.Disconnected;

            _uptimeWatch.Stop();
            _status.State = ServiceState.Stopped;

            _logger.LogInformation(
                "✓ CCStudio-Tunneler Worker stopped. Total runtime: {Uptime}, Messages processed: {Messages}",
                _status.Uptime.ToString(@"dd\.hh\:mm\:ss"),
                _messagesProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service shutdown");
        }

        await base.StopAsync(cancellationToken);
    }

    private List<OpcDaSource> GetDaSourcesList(TunnelerConfiguration config)
    {
        // If OpcDaSources is configured, use it
        if (config.OpcDaSources != null && config.OpcDaSources.Count > 0)
        {
            return config.OpcDaSources;
        }

        // Otherwise, fall back to legacy OpcDa configuration (backward compatibility)
        if (!string.IsNullOrEmpty(config.OpcDa.ServerProgId))
        {
            _logger.LogInformation("Using legacy OpcDa configuration (single server mode)");
            return new List<OpcDaSource>
            {
                new OpcDaSource
                {
                    Id = "default",
                    Name = $"{config.OpcDa.ServerProgId} on {config.OpcDa.ServerHost}",
                    UaNamespace = "", // No namespace prefix for legacy mode
                    Configuration = config.OpcDa,
                    Enabled = true
                }
            };
        }

        _logger.LogWarning("No OPC DA servers configured!");
        return new List<OpcDaSource>();
    }

    private OpcDaSource? GetSourceConfiguration(string serverId)
    {
        if (_configuration == null)
            return null;

        if (_configuration.OpcDaSources != null && _configuration.OpcDaSources.Count > 0)
        {
            return _configuration.OpcDaSources.FirstOrDefault(s => s.Id == serverId);
        }

        // Legacy mode
        if (serverId == "default")
        {
            return new OpcDaSource
            {
                Id = "default",
                Name = "Legacy Server",
                UaNamespace = "",
                Configuration = _configuration.OpcDa,
                Enabled = true
            };
        }

        return null;
    }

    private void ValidateConfiguration(TunnelerConfiguration config)
    {
        _logger.LogInformation("Validating configuration...");

        // Check OPC UA config
        if (string.IsNullOrEmpty(config.OpcUa.EndpointUrl))
        {
            throw new Exception("OPC UA EndpointUrl is required");
        }

        // Check OPC DA config
        var sources = GetDaSourcesList(config);
        if (sources.Count == 0)
        {
            throw new Exception("At least one OPC DA server must be configured");
        }

        foreach (var source in sources.Where(s => s.Enabled))
        {
            if (string.IsNullOrEmpty(source.Configuration.ServerProgId))
            {
                throw new Exception($"ServerProgId is required for server: {source.Name}");
            }

            _logger.LogDebug("✓ Validated server configuration: {Name}", source.Name);
        }

        _logger.LogInformation("✓ Configuration validated successfully");
    }
}
