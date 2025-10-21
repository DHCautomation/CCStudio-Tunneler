using System.Runtime.InteropServices;
using CCStudio.Tunneler.Core.Interfaces;
using CCStudio.Tunneler.Core.Models;
using Microsoft.Extensions.Logging;

namespace CCStudio.Tunneler.Service.OPC;

/// <summary>
/// OPC DA Client implementation using COM Interop (free/open-source approach)
/// Requires OPC Core Components installed on Windows
/// </summary>
public class OpcDaClient : IOpcDaClient
{
    private readonly ILogger _logger;
    private object? _opcServer;
    private object? _opcGroup;
    private OpcDaConfiguration? _configuration;
    private bool _isConnected;
    private readonly Dictionary<string, int> _itemHandles = new();
    private int _nextHandle = 1;

    public event EventHandler<TagValueChangedEventArgs>? TagValueChanged;
    public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

    public bool IsConnected => _isConnected;

    public OpcDaClient(ILogger? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public async Task<bool> ConnectAsync(OpcDaConfiguration configuration)
    {
        try
        {
            _configuration = configuration;
            _logger.LogInformation("Connecting to OPC DA Server: {ProgId} on {Host}",
                configuration.ServerProgId, configuration.ServerHost);

            // Create OPC Server instance via COM
            Type? serverType = Type.GetTypeFromProgID(configuration.ServerProgId, configuration.ServerHost);

            if (serverType == null)
            {
                _logger.LogError("OPC Server ProgID not found: {ProgId}", configuration.ServerProgId);
                RaiseConnectionStatusChanged(ConnectionStatus.Error, "Server ProgID not found");
                return false;
            }

            _opcServer = Activator.CreateInstance(serverType);

            if (_opcServer == null)
            {
                _logger.LogError("Failed to create OPC Server instance");
                RaiseConnectionStatusChanged(ConnectionStatus.Error, "Failed to create server instance");
                return false;
            }

            // Connect to server (IOPCServer::Connect equivalent)
            var result = InvokeComMethod(_opcServer, "Connect", configuration.ServerProgId, configuration.ServerHost);

            _isConnected = true;
            _logger.LogInformation("Successfully connected to OPC DA Server");
            RaiseConnectionStatusChanged(ConnectionStatus.Connected, "Connected successfully");

            // Create OPC Group for subscriptions
            await CreateGroupAsync();

            return true;
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM Error connecting to OPC DA Server: 0x{HResult:X}", ex.HResult);
            _isConnected = false;
            RaiseConnectionStatusChanged(ConnectionStatus.Error, $"COM Error: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to OPC DA Server");
            _isConnected = false;
            RaiseConnectionStatusChanged(ConnectionStatus.Error, ex.Message);
            return false;
        }
    }

    private async Task CreateGroupAsync()
    {
        if (_opcServer == null || _configuration == null)
            return;

        try
        {
            // Add OPC Group (IOPCServer::AddGroup equivalent)
            var groupName = "CCStudioTunnelerGroup";
            var updateRate = _configuration.UpdateRate;

            _opcGroup = InvokeComMethod(_opcServer, "AddGroup",
                groupName, true, updateRate, 0, 0, 0, 1033);

            if (_opcGroup != null)
            {
                _logger.LogInformation("Created OPC Group: {GroupName} with update rate {Rate}ms",
                    groupName, updateRate);

                // Set up data change callback if possible
                // Note: This is simplified - full implementation would use IOPCDataCallback
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating OPC Group");
        }

        await Task.CompletedTask;
    }

    public async Task<bool> SubscribeToTagsAsync(IEnumerable<string> tagNames)
    {
        if (_opcGroup == null)
        {
            _logger.LogWarning("Cannot subscribe - no OPC Group created");
            return false;
        }

        try
        {
            foreach (var tagName in tagNames)
            {
                // Add item to group (IOPCItemMgt::AddItems equivalent)
                var handle = _nextHandle++;
                _itemHandles[tagName] = handle;

                // Simplified - real implementation would use OPCITEMDEF structure
                InvokeComMethod(_opcGroup, "AddItem", tagName, handle);

                _logger.LogDebug("Subscribed to tag: {TagName}", tagName);
            }

            _logger.LogInformation("Subscribed to {Count} tags", tagNames.Count());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to tags");
            return false;
        }
    }

    public async Task UnsubscribeFromTagsAsync(IEnumerable<string> tagNames)
    {
        if (_opcGroup == null)
            return;

        try
        {
            foreach (var tagName in tagNames)
            {
                if (_itemHandles.TryGetValue(tagName, out var handle))
                {
                    InvokeComMethod(_opcGroup, "RemoveItem", handle);
                    _itemHandles.Remove(tagName);
                }
            }

            _logger.LogInformation("Unsubscribed from {Count} tags", tagNames.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from tags");
        }

        await Task.CompletedTask;
    }

    public async Task<TagValue?> ReadTagAsync(string tagName)
    {
        if (_opcGroup == null)
            return null;

        try
        {
            // Synchronous read (IOPCSyncIO::Read)
            var value = InvokeComMethod(_opcGroup, "Read", 1 /* OPC_DS_DEVICE */, 1, new[] { tagName });

            if (value != null)
            {
                return new TagValue
                {
                    TagName = tagName,
                    Value = value,
                    Quality = DataQuality.Good,
                    Timestamp = DateTime.UtcNow,
                    DataType = value.GetType().Name,
                    Source = "DA"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading tag: {TagName}", tagName);
        }

        return null;
    }

    public async Task<IEnumerable<TagValue>> ReadTagsAsync(IEnumerable<string> tagNames)
    {
        var results = new List<TagValue>();

        foreach (var tagName in tagNames)
        {
            var value = await ReadTagAsync(tagName);
            if (value != null)
                results.Add(value);
        }

        return results;
    }

    public async Task<bool> WriteTagAsync(string tagName, object value)
    {
        if (_opcGroup == null)
        {
            _logger.LogWarning("Cannot write - no OPC Group created");
            return false;
        }

        try
        {
            // Synchronous write (IOPCSyncIO::Write)
            InvokeComMethod(_opcGroup, "Write", 1, new[] { tagName }, new[] { value });

            _logger.LogDebug("Wrote value to tag {TagName}: {Value}", tagName, value);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to tag: {TagName}", tagName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> BrowseTagsAsync(string? path = null)
    {
        var tags = new List<string>();

        if (_opcServer == null)
        {
            _logger.LogWarning("Cannot browse - not connected to server");
            return tags;
        }

        try
        {
            // Browse OPC address space (IOPCBrowseServerAddressSpace)
            // This is simplified - real implementation would walk the tree

            var browser = InvokeComMethod(_opcServer, "CreateBrowser");
            if (browser != null)
            {
                // Get flat list or hierarchical based on path
                _logger.LogInformation("Browsing OPC address space from: {Path}", path ?? "root");

                // Simplified browsing - would need full COM interop for complete implementation
                // For now, return empty list - this requires complex COM marshaling
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error browsing OPC address space");
        }

        return await Task.FromResult(tags);
    }

    public async Task DisconnectAsync()
    {
        try
        {
            if (_opcGroup != null)
            {
                InvokeComMethod(_opcServer, "RemoveGroup", _opcGroup);
                Marshal.ReleaseComObject(_opcGroup);
                _opcGroup = null;
            }

            if (_opcServer != null)
            {
                InvokeComMethod(_opcServer, "Disconnect");
                Marshal.ReleaseComObject(_opcServer);
                _opcServer = null;
            }

            _isConnected = false;
            _itemHandles.Clear();

            _logger.LogInformation("Disconnected from OPC DA Server");
            RaiseConnectionStatusChanged(ConnectionStatus.Disconnected, "Disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from OPC DA Server");
        }

        await Task.CompletedTask;
    }

    private object? InvokeComMethod(object comObject, string methodName, params object[] parameters)
    {
        try
        {
            var type = comObject.GetType();
            return type.InvokeMember(methodName,
                System.Reflection.BindingFlags.InvokeMethod,
                null, comObject, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking COM method: {Method}", methodName);
            throw;
        }
    }

    private void RaiseConnectionStatusChanged(ConnectionStatus status, string? message)
    {
        ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
        {
            Status = status,
            Message = message
        });
    }

    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}
