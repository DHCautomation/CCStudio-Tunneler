namespace CCStudio.Tunneler.Core.Models;

/// <summary>
/// Configuration for OPC UA server
/// </summary>
public class OpcUaConfiguration
{
    /// <summary>
    /// OPC UA server port (default: 4840)
    /// </summary>
    public int ServerPort { get; set; } = 4840;

    /// <summary>
    /// OPC UA server name
    /// </summary>
    public string ServerName { get; set; } = "CCStudio Tunneler";

    /// <summary>
    /// OPC UA endpoint URL
    /// </summary>
    public string EndpointUrl { get; set; } = "opc.tcp://localhost:4840";

    /// <summary>
    /// Security mode (None, Sign, SignAndEncrypt)
    /// </summary>
    public SecurityMode SecurityMode { get; set; } = SecurityMode.None;

    /// <summary>
    /// Allow anonymous access
    /// </summary>
    public bool AllowAnonymous { get; set; } = true;

    /// <summary>
    /// Username for authentication (if not anonymous)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for authentication (if not anonymous)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Application name shown to clients
    /// </summary>
    public string ApplicationName { get; set; } = "CCStudio-Tunneler";

    /// <summary>
    /// Application URI
    /// </summary>
    public string ApplicationUri { get; set; } = "urn:DHCAutomation:CCStudio-Tunneler";

    /// <summary>
    /// Product URI
    /// </summary>
    public string ProductUri { get; set; } = "https://dhcautomation.com/ccstudio-tunneler";

    /// <summary>
    /// Maximum number of simultaneous sessions
    /// </summary>
    public int MaxSessionCount { get; set; } = 100;

    /// <summary>
    /// Maximum number of subscriptions per session
    /// </summary>
    public int MaxSubscriptionCount { get; set; } = 100;

    /// <summary>
    /// Minimum publishing interval in milliseconds
    /// </summary>
    public int MinPublishingInterval { get; set; } = 100;

    /// <summary>
    /// Maximum publishing interval in milliseconds
    /// </summary>
    public int MaxPublishingInterval { get; set; } = 10000;
}

/// <summary>
/// OPC UA security modes
/// </summary>
public enum SecurityMode
{
    None,
    Sign,
    SignAndEncrypt
}
