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
