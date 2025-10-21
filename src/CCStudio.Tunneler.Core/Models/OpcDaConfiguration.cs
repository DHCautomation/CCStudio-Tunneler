namespace CCStudio.Tunneler.Core.Models;

/// <summary>
/// Configuration for OPC DA client connection
/// </summary>
public class OpcDaConfiguration
{
    /// <summary>
    /// OPC DA Server ProgID (e.g., "Matrikon.OPC.Simulation.1")
    /// </summary>
    public string ServerProgId { get; set; } = string.Empty;

    /// <summary>
    /// Server host name or IP address (use "localhost" for local server)
    /// </summary>
    public string ServerHost { get; set; } = "localhost";

    /// <summary>
    /// Update rate in milliseconds (default: 1000ms)
    /// </summary>
    public int UpdateRate { get; set; } = 1000;

    /// <summary>
    /// Dead band percentage (0-100) - only send updates if value changes by this percentage
    /// </summary>
    public float DeadBand { get; set; } = 0.0f;

    /// <summary>
    /// List of tags to subscribe to (* for all tags)
    /// </summary>
    public List<string> Tags { get; set; } = new() { "*" };

    /// <summary>
    /// Tag filters (patterns to include)
    /// </summary>
    public List<string> IncludeFilters { get; set; } = new();

    /// <summary>
    /// Tag filters (patterns to exclude)
    /// </summary>
    public List<string> ExcludeFilters { get; set; } = new();

    /// <summary>
    /// Enable automatic tag discovery
    /// </summary>
    public bool AutoDiscoverTags { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Reconnection attempts before giving up (0 = infinite)
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 0;

    /// <summary>
    /// Delay between reconnection attempts in seconds
    /// </summary>
    public int ReconnectDelay { get; set; } = 5;
}
