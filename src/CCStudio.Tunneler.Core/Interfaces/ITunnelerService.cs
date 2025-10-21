using CCStudio.Tunneler.Core.Models;

namespace CCStudio.Tunneler.Core.Interfaces;

/// <summary>
/// Main tunneler service interface - coordinates OPC DA and OPC UA
/// </summary>
public interface ITunnelerService
{
    /// <summary>
    /// Start the tunneler service
    /// </summary>
    Task<bool> StartAsync();

    /// <summary>
    /// Stop the tunneler service
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Restart the tunneler service
    /// </summary>
    Task<bool> RestartAsync();

    /// <summary>
    /// Get current service status
    /// </summary>
    Task<TunnelerStatus> GetStatusAsync();

    /// <summary>
    /// Get performance metrics
    /// </summary>
    Task<PerformanceMetrics> GetMetricsAsync();

    /// <summary>
    /// Reload configuration
    /// </summary>
    Task<bool> ReloadConfigurationAsync();

    /// <summary>
    /// Check if service is running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Event raised when service status changes
    /// </summary>
    event EventHandler<ServiceStatusChangedEventArgs>? StatusChanged;
}

/// <summary>
/// Event args for service status changes
/// </summary>
public class ServiceStatusChangedEventArgs : EventArgs
{
    public ServiceState OldState { get; set; }
    public ServiceState NewState { get; set; }
    public string? Message { get; set; }
}
