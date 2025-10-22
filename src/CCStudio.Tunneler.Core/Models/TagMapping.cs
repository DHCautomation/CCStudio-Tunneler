namespace CCStudio.Tunneler.Core.Models;

/// <summary>
/// Maps an OPC DA tag to an OPC UA node
/// </summary>
public class TagMapping
{
    /// <summary>
    /// OPC DA tag name (source)
    /// </summary>
    public string DaTagName { get; set; } = string.Empty;

    /// <summary>
    /// OPC UA node name (destination)
    /// </summary>
    public string UaNodeName { get; set; } = string.Empty;

    /// <summary>
    /// OPC UA namespace index (default: 2)
    /// </summary>
    public ushort NamespaceIndex { get; set; } = 2;

    /// <summary>
    /// Enable this mapping
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Access level (Read, Write, ReadWrite)
    /// </summary>
    public AccessLevel AccessLevel { get; set; } = AccessLevel.ReadWrite;

    /// <summary>
    /// Data type override (null = auto-detect)
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Scaling factor for numeric values (null = no scaling)
    /// </summary>
    public double? ScaleFactor { get; set; }

    /// <summary>
    /// Offset for numeric values (null = no offset)
    /// </summary>
    public double? Offset { get; set; }

    /// <summary>
    /// Description for the OPC UA node
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Engineering units (e.g., "degC", "PSI", "RPM")
    /// </summary>
    public string? EngineeringUnits { get; set; }

    /// <summary>
    /// Update rate for this tag in milliseconds (null = use global rate)
    /// Allows per-tag optimization (e.g., slow-changing temps: 60000ms, critical alarms: 1000ms)
    /// </summary>
    public int? UpdateRate { get; set; }

    /// <summary>
    /// Deadband percentage for this tag (0-100, null = use global deadband)
    /// Only send updates if value changes by this percentage
    /// </summary>
    public float? Deadband { get; set; }

    /// <summary>
    /// Server ID this tag belongs to (for multi-server support)
    /// </summary>
    public string? ServerId { get; set; }

    /// <summary>
    /// Minimum valid value (for validation)
    /// </summary>
    public double? MinValue { get; set; }

    /// <summary>
    /// Maximum valid value (for validation)
    /// </summary>
    public double? MaxValue { get; set; }
}

/// <summary>
/// Access level for OPC tags
/// </summary>
public enum AccessLevel
{
    Read,
    Write,
    ReadWrite
}
