namespace CCStudio.Tunneler.Core.Models;

/// <summary>
/// Performance metrics for monitoring
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Average latency in milliseconds
    /// </summary>
    public double AverageLatency { get; set; }

    /// <summary>
    /// Maximum latency in milliseconds
    /// </summary>
    public double MaxLatency { get; set; }

    /// <summary>
    /// Minimum latency in milliseconds
    /// </summary>
    public double MinLatency { get; set; }

    /// <summary>
    /// Current throughput (values/second)
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public double MemoryUsageMB { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Number of reconnection attempts
    /// </summary>
    public int ReconnectAttempts { get; set; }

    /// <summary>
    /// Number of successful writes
    /// </summary>
    public long SuccessfulWrites { get; set; }

    /// <summary>
    /// Number of failed writes
    /// </summary>
    public long FailedWrites { get; set; }

    /// <summary>
    /// Timestamp of metrics collection
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
