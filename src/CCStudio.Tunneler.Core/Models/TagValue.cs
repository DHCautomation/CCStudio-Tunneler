namespace CCStudio.Tunneler.Core.Models;

/// <summary>
/// Represents a tag value with quality and timestamp
/// </summary>
public class TagValue
{
    /// <summary>
    /// Tag name
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Current value
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Data quality
    /// </summary>
    public DataQuality Quality { get; set; } = DataQuality.Good;

    /// <summary>
    /// Timestamp of the value
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data type name
    /// </summary>
    public string DataType { get; set; } = "Unknown";

    /// <summary>
    /// Source of the value (DA or UA)
    /// </summary>
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Data quality enumeration
/// </summary>
public enum DataQuality
{
    /// <summary>
    /// Good quality data
    /// </summary>
    Good = 0,

    /// <summary>
    /// Bad quality - sensor failure or disconnected
    /// </summary>
    Bad = 1,

    /// <summary>
    /// Uncertain quality - sensor out of range or last known value
    /// </summary>
    Uncertain = 2
}
