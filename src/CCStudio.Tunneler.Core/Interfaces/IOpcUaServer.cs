using CCStudio.Tunneler.Core.Models;

namespace CCStudio.Tunneler.Core.Interfaces;

/// <summary>
/// Interface for OPC UA server operations
/// </summary>
public interface IOpcUaServer : IDisposable
{
    /// <summary>
    /// Start the OPC UA server
    /// </summary>
    Task<bool> StartAsync(OpcUaConfiguration configuration);

    /// <summary>
    /// Stop the OPC UA server
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Check if server is running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Add or update a tag/node in the OPC UA address space
    /// </summary>
    Task<bool> AddOrUpdateNodeAsync(TagMapping mapping, TagValue? initialValue = null);

    /// <summary>
    /// Remove a node from the address space
    /// </summary>
    Task<bool> RemoveNodeAsync(string nodeName);

    /// <summary>
    /// Update a node value
    /// </summary>
    Task<bool> UpdateNodeValueAsync(string nodeName, TagValue value);

    /// <summary>
    /// Get all registered nodes
    /// </summary>
    Task<IEnumerable<string>> GetRegisteredNodesAsync();

    /// <summary>
    /// Get number of connected clients
    /// </summary>
    int GetConnectedClientCount();

    /// <summary>
    /// Event raised when a client writes to a node
    /// </summary>
    event EventHandler<NodeWriteEventArgs>? NodeWritten;

    /// <summary>
    /// Event raised when a client connects
    /// </summary>
    event EventHandler<ClientConnectedEventArgs>? ClientConnected;

    /// <summary>
    /// Event raised when a client disconnects
    /// </summary>
    event EventHandler<ClientDisconnectedEventArgs>? ClientDisconnected;
}

/// <summary>
/// Event args for node write operations
/// </summary>
public class NodeWriteEventArgs : EventArgs
{
    public string NodeName { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? ClientId { get; set; }
}

/// <summary>
/// Event args for client connection
/// </summary>
public class ClientConnectedEventArgs : EventArgs
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event args for client disconnection
/// </summary>
public class ClientDisconnectedEventArgs : EventArgs
{
    public string ClientId { get; set; } = string.Empty;
    public DateTime DisconnectedAt { get; set; } = DateTime.UtcNow;
}
