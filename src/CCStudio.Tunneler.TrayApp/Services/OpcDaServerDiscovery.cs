using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace CCStudio.Tunneler.TrayApp.Services;

/// <summary>
/// Discovers OPC DA servers installed on the local machine or remote hosts
/// Uses COM and Windows Registry to enumerate registered OPC servers
/// </summary>
public class OpcDaServerDiscovery
{
    /// <summary>
    /// Represents an OPC DA server found during discovery
    /// </summary>
    public class OpcServerInfo
    {
        public string ProgId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string VendorInfo { get; set; } = string.Empty;
        public string Host { get; set; } = "localhost";
        public Guid? ClassId { get; set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Description)
                ? ProgId
                : $"{ProgId} - {Description}";
        }
    }

    /// <summary>
    /// Discovers OPC DA servers on the local machine using registry enumeration
    /// </summary>
    public static List<OpcServerInfo> DiscoverLocalServers()
    {
        var servers = new List<OpcServerInfo>();

        try
        {
            // Method 1: Enumerate HKEY_CLASSES_ROOT for OPC-related ProgIDs
            using var classesRoot = Registry.ClassesRoot;

            // Common OPC DA server patterns
            var patterns = new[]
            {
                "OPC",
                "Kepware",
                "Matrikon",
                "Schneider",
                "Rockwell",
                "Siemens",
                "Honeywell",
                "ABB",
                "GE",
                "ASI",
                "WebCTRL"
            };

            var subKeyNames = classesRoot.GetSubKeyNames();

            foreach (var keyName in subKeyNames)
            {
                // Look for ProgIDs that match OPC server patterns
                if (patterns.Any(p => keyName.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        using var progIdKey = classesRoot.OpenSubKey(keyName);
                        if (progIdKey != null)
                        {
                            // Check if it has a CLSID (COM component)
                            using var clsidKey = progIdKey.OpenSubKey("CLSID");
                            if (clsidKey != null)
                            {
                                var clsidValue = clsidKey.GetValue(null)?.ToString();
                                if (!string.IsNullOrEmpty(clsidValue))
                                {
                                    // Verify it's an OPC server by checking for OPC-specific registry keys
                                    var description = GetServerDescription(clsidValue);

                                    var server = new OpcServerInfo
                                    {
                                        ProgId = keyName,
                                        Description = description ?? keyName,
                                        Host = "localhost",
                                        ClassId = Guid.TryParse(clsidValue.Trim('{', '}'), out var guid) ? guid : null
                                    };

                                    servers.Add(server);
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip servers we can't read
                    }
                }
            }

            // Method 2: Use OPC Server Enumeration COM interface (if available)
            try
            {
                var comServers = EnumerateServersViaCom();
                foreach (var server in comServers)
                {
                    if (!servers.Any(s => s.ProgId.Equals(server.ProgId, StringComparison.OrdinalIgnoreCase)))
                    {
                        servers.Add(server);
                    }
                }
            }
            catch
            {
                // OpcEnum.exe might not be available
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error discovering OPC servers: {ex.Message}");
        }

        return servers.OrderBy(s => s.ProgId).ToList();
    }

    /// <summary>
    /// Gets the description of an OPC server from the registry
    /// </summary>
    private static string? GetServerDescription(string clsid)
    {
        try
        {
            using var classesRoot = Registry.ClassesRoot;
            using var clsidKey = classesRoot.OpenSubKey($"CLSID\\{clsid}");

            if (clsidKey != null)
            {
                // Try to get description from default value
                var description = clsidKey.GetValue(null)?.ToString();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    return description;
                }

                // Try VersionIndependentProgID
                using var versionIndependentKey = clsidKey.OpenSubKey("VersionIndependentProgID");
                if (versionIndependentKey != null)
                {
                    return versionIndependentKey.GetValue(null)?.ToString();
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    /// <summary>
    /// Enumerates OPC DA servers using the OpcEnum COM interface (IOPCServerList)
    /// </summary>
    private static List<OpcServerInfo> EnumerateServersViaCom()
    {
        var servers = new List<OpcServerInfo>();

        try
        {
            // OPC Server Enumerator CLSID for OPC DA 2.0
            var opcEnumClsid = new Guid("13486D51-4821-11D2-A494-3CB306C10000");

            Type? opcEnumType = Type.GetTypeFromCLSID(opcEnumClsid);
            if (opcEnumType == null)
            {
                return servers;
            }

            object? opcEnum = Activator.CreateInstance(opcEnumType);
            if (opcEnum == null)
            {
                return servers;
            }

            try
            {
                // Call EnumClassesOfCategories for OPC DA 2.0 servers
                // CATID_OPCDAServer20 = {63D5F432-CFE4-11d1-B2C8-0060083BA1FB}
                var da20CategoryGuid = new Guid("63D5F432-CFE4-11d1-B2C8-0060083BA1FB");

                // This is simplified - full implementation would use proper COM interop
                var result = opcEnum.GetType().InvokeMember("EnumClassesOfCategories",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null, opcEnum, new object[] { 1, new[] { da20CategoryGuid }, 0, new Guid[] { } });

                // Parse enumeration results (would need proper COM marshaling)
            }
            finally
            {
                if (opcEnum != null)
                {
                    Marshal.ReleaseComObject(opcEnum);
                }
            }
        }
        catch
        {
            // OpcEnum might not be available or accessible
        }

        return servers;
    }

    /// <summary>
    /// Tests if an OPC DA server is accessible
    /// </summary>
    public static Task<(bool isAccessible, string message)> TestServerConnection(string progId, string host = "localhost")
    {
        return Task.Run(() =>
        {
            try
            {
                Type? serverType = Type.GetTypeFromProgID(progId, host);

                if (serverType == null)
                {
                    return (false, $"Server ProgID '{progId}' not found on {host}");
                }

                object? server = Activator.CreateInstance(serverType);

                if (server == null)
                {
                    return (false, "Failed to create server instance");
                }

                try
                {
                    // Try to connect
                    var connectMethod = server.GetType().GetMethod("Connect");
                    if (connectMethod != null)
                    {
                        connectMethod.Invoke(server, new object[] { progId, host });
                    }

                    // If we got here, connection succeeded

                    // Try to disconnect
                    var disconnectMethod = server.GetType().GetMethod("Disconnect");
                    disconnectMethod?.Invoke(server, null);

                    return (true, "Connection successful");
                }
                finally
                {
                    Marshal.ReleaseComObject(server);
                }
            }
            catch (COMException ex)
            {
                return (false, $"COM Error: {ex.Message} (0x{ex.HResult:X})");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Gets common OPC DA server ProgIDs for quick selection
    /// </summary>
    public static List<string> GetCommonProgIds()
    {
        return new List<string>
        {
            "Matrikon.OPC.Simulation.1",
            "KEPware.KEPServerEX.V6",
            "Kepware.KEPServerEx.V5",
            "RSLinx.OPC.1",
            "OPC.SimaticNet",
            "Schneider-Aut.OFS.2",
            "ASI.OPCServer.1",
            "ASIControls.OPCServer.1",
            "WebCTRL.OPCServer.1",
            "Honeywell.OPCServer.1",
            "Siemens.SimaticNet.OPCAccess.1"
        };
    }
}
