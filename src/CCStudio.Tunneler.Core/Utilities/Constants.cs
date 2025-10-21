namespace CCStudio.Tunneler.Core.Utilities;

/// <summary>
/// Application-wide constants
/// </summary>
public static class Constants
{
    /// <summary>
    /// Application name
    /// </summary>
    public const string ApplicationName = "CCStudio-Tunneler";

    /// <summary>
    /// Company name
    /// </summary>
    public const string CompanyName = "DHC Automation and Controls";

    /// <summary>
    /// Application version
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// Default configuration path
    /// </summary>
    public const string DefaultConfigPath = @"C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json";

    /// <summary>
    /// Default log path
    /// </summary>
    public const string DefaultLogPath = @"C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs";

    /// <summary>
    /// Default OPC UA port
    /// </summary>
    public const int DefaultOpcUaPort = 4840;

    /// <summary>
    /// Default OPC DA update rate in milliseconds
    /// </summary>
    public const int DefaultUpdateRate = 1000;

    /// <summary>
    /// Service name for Windows Service
    /// </summary>
    public const string ServiceName = "CCStudioTunneler";

    /// <summary>
    /// Service display name
    /// </summary>
    public const string ServiceDisplayName = "CCStudio-Tunneler Service";

    /// <summary>
    /// Service description
    /// </summary>
    public const string ServiceDescription = "OPC DA to OPC UA Bridge by DHC Automation and Controls";

    /// <summary>
    /// Named pipe name for IPC communication between service and tray app
    /// </summary>
    public const string NamedPipeName = "CCStudioTunnelerPipe";
}
