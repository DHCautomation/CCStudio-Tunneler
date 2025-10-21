namespace CCStudio.Tunneler.Core.Models;

/// <summary>
/// Current status of the tunneler service
/// </summary>
public class TunnelerStatus
{
    /// <summary>
    /// Service running state
    /// </summary>
    public ServiceState State { get; set; } = ServiceState.Stopped;

    /// <summary>
    /// OPC DA connection status
    /// </summary>
    public ConnectionStatus DaConnectionStatus { get; set; } = ConnectionStatus.Disconnected;

    /// <summary>
    /// OPC UA server status
    /// </summary>
    public ConnectionStatus UaServerStatus { get; set; } = ConnectionStatus.Disconnected;

    /// <summary>
    /// Number of active tags
    /// </summary>
    public int ActiveTagCount { get; set; } = 0;

    /// <summary>
    /// Number of connected OPC UA clients
    /// </summary>
    public int ConnectedClients { get; set; } = 0;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total messages processed
    /// </summary>
    public long TotalMessagesProcessed { get; set; } = 0;

    /// <summary>
    /// Current messages per second
    /// </summary>
    public double MessagesPerSecond { get; set; } = 0;

    /// <summary>
    /// Total errors since start
    /// </summary>
    public int ErrorCount { get; set; } = 0;

    /// <summary>
    /// Last error message
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Service uptime
    /// </summary>
    public TimeSpan Uptime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Service start time
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Current configuration loaded
    /// </summary>
    public bool ConfigurationLoaded { get; set; } = false;
}

/// <summary>
/// Service running state
/// </summary>
public enum ServiceState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Error
}

/// <summary>
/// Connection status
/// </summary>
public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error,
    Reconnecting
}
