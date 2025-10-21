using CCStudio.Tunneler.Core.Models;

namespace CCStudio.Tunneler.Core.Interfaces;

/// <summary>
/// Interface for OPC DA client operations
/// </summary>
public interface IOpcDaClient : IDisposable
{
    /// <summary>
    /// Connect to the OPC DA server
    /// </summary>
    Task<bool> ConnectAsync(OpcDaConfiguration configuration);

    /// <summary>
    /// Disconnect from the OPC DA server
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Check if connected to server
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Subscribe to tags
    /// </summary>
    Task<bool> SubscribeToTagsAsync(IEnumerable<string> tagNames);

    /// <summary>
    /// Unsubscribe from tags
    /// </summary>
    Task UnsubscribeFromTagsAsync(IEnumerable<string> tagNames);

    /// <summary>
    /// Read a single tag value
    /// </summary>
    Task<TagValue?> ReadTagAsync(string tagName);

    /// <summary>
    /// Read multiple tag values
    /// </summary>
    Task<IEnumerable<TagValue>> ReadTagsAsync(IEnumerable<string> tagNames);

    /// <summary>
    /// Write a value to a tag
    /// </summary>
    Task<bool> WriteTagAsync(string tagName, object value);

    /// <summary>
    /// Browse available tags on the server
    /// </summary>
    Task<IEnumerable<string>> BrowseTagsAsync(string? path = null);

    /// <summary>
    /// Event raised when tag values change
    /// </summary>
    event EventHandler<TagValueChangedEventArgs>? TagValueChanged;

    /// <summary>
    /// Event raised when connection status changes
    /// </summary>
    event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;
}

/// <summary>
/// Event args for tag value changes
/// </summary>
public class TagValueChangedEventArgs : EventArgs
{
    public TagValue TagValue { get; set; } = new();
}

/// <summary>
/// Event args for connection status changes
/// </summary>
public class ConnectionStatusChangedEventArgs : EventArgs
{
    public ConnectionStatus Status { get; set; }
    public string? Message { get; set; }
}
