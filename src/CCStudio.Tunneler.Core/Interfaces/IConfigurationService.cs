using CCStudio.Tunneler.Core.Models;

namespace CCStudio.Tunneler.Core.Interfaces;

/// <summary>
/// Interface for configuration management
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Load configuration from file
    /// </summary>
    Task<TunnelerConfiguration> LoadConfigurationAsync(string? filePath = null);

    /// <summary>
    /// Save configuration to file
    /// </summary>
    Task<bool> SaveConfigurationAsync(TunnelerConfiguration configuration, string? filePath = null);

    /// <summary>
    /// Get the current configuration
    /// </summary>
    TunnelerConfiguration GetConfiguration();

    /// <summary>
    /// Validate configuration
    /// </summary>
    Task<(bool isValid, List<string> errors)> ValidateConfigurationAsync(TunnelerConfiguration configuration);

    /// <summary>
    /// Export configuration to JSON string
    /// </summary>
    string ExportConfiguration(TunnelerConfiguration configuration);

    /// <summary>
    /// Import configuration from JSON string
    /// </summary>
    Task<TunnelerConfiguration?> ImportConfigurationAsync(string json);

    /// <summary>
    /// Get default configuration
    /// </summary>
    TunnelerConfiguration GetDefaultConfiguration();

    /// <summary>
    /// Event raised when configuration changes
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}

/// <summary>
/// Event args for configuration changes
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public TunnelerConfiguration OldConfiguration { get; set; } = new();
    public TunnelerConfiguration NewConfiguration { get; set; } = new();
}
