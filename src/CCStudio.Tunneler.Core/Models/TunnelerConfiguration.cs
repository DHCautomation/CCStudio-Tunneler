namespace CCStudio.Tunneler.Core.Models;

/// <summary>
/// Main configuration for CCStudio-Tunneler
/// </summary>
public class TunnelerConfiguration
{
    /// <summary>
    /// OPC DA client configuration (single server - legacy)
    /// Maintained for backward compatibility
    /// </summary>
    public OpcDaConfiguration OpcDa { get; set; } = new();

    /// <summary>
    /// OPC DA server sources (multi-server support)
    /// If empty, will use OpcDa property for single server
    /// </summary>
    public List<OpcDaSource> OpcDaSources { get; set; } = new();

    /// <summary>
    /// OPC UA server configuration
    /// </summary>
    public OpcUaConfiguration OpcUa { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();

    /// <summary>
    /// Tag mappings (OPC DA tag -> OPC UA node)
    /// </summary>
    public List<TagMapping> TagMappings { get; set; } = new();

    /// <summary>
    /// Enable performance metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Service display name
    /// </summary>
    public string ServiceName { get; set; } = "CCStudio-Tunneler";

    /// <summary>
    /// Service description
    /// </summary>
    public string ServiceDescription { get; set; } = "OPC DA to OPC UA Bridge by DHC Automation and Controls";
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Minimum log level (Verbose, Debug, Information, Warning, Error, Fatal)
    /// </summary>
    public string Level { get; set; } = "Information";

    /// <summary>
    /// Log file path
    /// </summary>
    public string Path { get; set; } = @"C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs";

    /// <summary>
    /// Log file retention in days
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Maximum log file size in MB
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Enable console logging
    /// </summary>
    public bool EnableConsole { get; set; } = true;
}
