using CCStudio.Tunneler.Core.Interfaces;
using CCStudio.Tunneler.Core.Models;
using CCStudio.Tunneler.Core.Services;
using CCStudio.Tunneler.Core.Utilities;
using CCStudio.Tunneler.Service.OPC;

namespace CCStudio.Tunneler.Service;

/// <summary>
/// Main worker service that coordinates OPC DA client and OPC UA server
/// </summary>
public class TunnelerWorker : BackgroundService
{
    private readonly ILogger<TunnelerWorker> _logger;
    private readonly ConfigurationService _configService;
    private TunnelerConfiguration? _configuration;
    private TunnelerStatus _status;
    private DateTime _startTime;
    private long _messagesProcessed;
    private System.Diagnostics.Stopwatch _uptimeWatch;

    // OPC implementations
    private IOpcDaClient? _daClient;
    private IOpcUaServer? _uaServer;

    public TunnelerWorker(ILogger<TunnelerWorker> logger, ConfigurationService configService)
    {
        _logger = logger;
        _configService = configService;
        _status = new TunnelerStatus();
        _uptimeWatch = new System.Diagnostics.Stopwatch();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CCStudio-Tunneler Worker starting at: {Time}", DateTimeOffset.Now);
        _startTime = DateTime.UtcNow;
        _uptimeWatch.Start();

        try
        {
            // Load configuration
            _logger.LogInformation("Loading configuration...");
            _configuration = await _configService.LoadConfigurationAsync();
            _status.ConfigurationLoaded = true;

            // Reconfigure logger with loaded settings
            if (_configuration?.Logging != null)
            {
                LoggerFactory.EnsureLogDirectoryExists(_configuration.Logging.Path);
                _logger.LogInformation("Logging configured: Level={Level}, Path={Path}",
                    _configuration.Logging.Level, _configuration.Logging.Path);
            }

            _status.State = ServiceState.Starting;

            // Initialize OPC UA Server first
            _logger.LogInformation("Starting OPC UA Server...");
            _logger.LogInformation("OPC UA Endpoint: {EndpointUrl}", _configuration.OpcUa.EndpointUrl);

            _uaServer = new OpcUaServer(_logger);
            _uaServer.NodeWritten += OnUaNodeWritten;
            _uaServer.ClientConnected += OnClientConnected;
            _uaServer.ClientDisconnected += OnClientDisconnected;

            if (await _uaServer.StartAsync(_configuration.OpcUa))
            {
                _status.UaServerStatus = ConnectionStatus.Connected;
                _logger.LogInformation("OPC UA Server started successfully");
            }
            else
            {
                _status.UaServerStatus = ConnectionStatus.Error;
                _logger.LogError("Failed to start OPC UA Server");
                throw new Exception("OPC UA Server failed to start");
            }

            // Initialize OPC DA Client
            _logger.LogInformation("Connecting to OPC DA Server...");
            _logger.LogInformation("OPC DA Server: {ServerProgId} on {ServerHost}",
                _configuration.OpcDa.ServerProgId, _configuration.OpcDa.ServerHost);

            _daClient = new OpcDaClient(_logger);
            _daClient.TagValueChanged += OnDaTagValueChanged;
            _daClient.ConnectionStatusChanged += OnDaConnectionStatusChanged;

            if (await _daClient.ConnectAsync(_configuration.OpcDa))
            {
                _status.DaConnectionStatus = ConnectionStatus.Connected;
                _logger.LogInformation("Connected to OPC DA Server successfully");

                // Subscribe to configured tags
                await SubscribeToTagsAsync();
            }
            else
            {
                _status.DaConnectionStatus = ConnectionStatus.Error;
                _logger.LogWarning("Failed to connect to OPC DA Server - will retry later");
                // Don't throw - allow service to run and retry connection
            }

            _status.State = ServiceState.Running;
            _status.StartTime = _startTime;
            _logger.LogInformation("CCStudio-Tunneler is now running and bridging OPC DA to OPC UA");

            // Main service loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Update status
                    _status.Uptime = _uptimeWatch.Elapsed;
                    _status.LastUpdate = DateTime.UtcNow;
                    _status.TotalMessagesProcessed = _messagesProcessed;
                    _status.ConnectedClients = _uaServer?.GetConnectedClientCount() ?? 0;
                    _status.ActiveTagCount = _configuration?.TagMappings.Count(m => m.Enabled) ?? 0;

                    // Check OPC DA connection and attempt reconnect if needed
                    if (_daClient != null && !_daClient.IsConnected &&
                        _status.DaConnectionStatus != ConnectionStatus.Connecting)
                    {
                        _logger.LogInformation("OPC DA disconnected, attempting to reconnect...");
                        _status.DaConnectionStatus = ConnectionStatus.Reconnecting;

                        if (await _daClient.ConnectAsync(_configuration!.OpcDa))
                        {
                            _status.DaConnectionStatus = ConnectionStatus.Connected;
                            _logger.LogInformation("Reconnected to OPC DA Server");
                            await SubscribeToTagsAsync();
                        }
                        else
                        {
                            _status.DaConnectionStatus = ConnectionStatus.Error;
                            _logger.LogWarning("Reconnection failed, will retry later");
                        }
                    }

                    // Log periodic heartbeat
                    if (_messagesProcessed % 1000 == 0 && _messagesProcessed > 0)
                    {
                        _logger.LogInformation(
                            "Service heartbeat - Uptime: {Uptime}, Messages: {Messages}, Clients: {Clients}, Tags: {Tags}",
                            _status.Uptime.ToFriendlyString(),
                            _messagesProcessed,
                            _status.ConnectedClients,
                            _status.ActiveTagCount);
                    }

                    // Wait before next iteration
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error in service loop");
                    _status.ErrorCount++;
                    _status.LastError = ex.Message;
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in worker service");
            _status.State = ServiceState.Error;
            _status.LastError = ex.Message;
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CCStudio-Tunneler Worker stopping...");
        _status.State = ServiceState.Stopping;

        try
        {
            // Disconnect OPC DA Client
            if (_daClient != null)
            {
                _logger.LogInformation("Disconnecting OPC DA Client...");
                await _daClient.DisconnectAsync();
                _daClient.Dispose();
                _daClient = null;
            }
            _status.DaConnectionStatus = ConnectionStatus.Disconnected;

            // Stop OPC UA Server
            if (_uaServer != null)
            {
                _logger.LogInformation("Stopping OPC UA Server...");
                await _uaServer.StopAsync();
                _uaServer.Dispose();
                _uaServer = null;
            }
            _status.UaServerStatus = ConnectionStatus.Disconnected;

            _uptimeWatch.Stop();
            _status.State = ServiceState.Stopped;

            _logger.LogInformation(
                "CCStudio-Tunneler Worker stopped. Total runtime: {Uptime}, Messages processed: {Messages}",
                _status.Uptime.ToFriendlyString(),
                _messagesProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during service shutdown");
        }

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Subscribe to configured OPC DA tags
    /// </summary>
    private async Task SubscribeToTagsAsync()
    {
        if (_daClient == null || _uaServer == null || _configuration == null)
            return;

        try
        {
            // If no explicit mappings, subscribe to all tags matching filters
            if (_configuration.TagMappings.Count == 0 && _configuration.OpcDa.AutoDiscoverTags)
            {
                _logger.LogInformation("No tag mappings configured - browsing OPC DA server...");
                var availableTags = await _daClient.BrowseTagsAsync();

                foreach (var tag in availableTags.Take(100)) // Limit to 100 for initial discovery
                {
                    _configuration.TagMappings.Add(new TagMapping
                    {
                        DaTagName = tag,
                        UaNodeName = tag.Replace('.', '/'),
                        Enabled = true,
                        AccessLevel = AccessLevel.ReadWrite,
                        NamespaceIndex = 2
                    });
                }

                _logger.LogInformation("Auto-discovered {Count} tags", _configuration.TagMappings.Count);
            }

            // Subscribe to tags in OPC DA
            var enabledTags = _configuration.TagMappings.Where(m => m.Enabled).Select(m => m.DaTagName);
            await _daClient.SubscribeToTagsAsync(enabledTags);

            // Create corresponding nodes in OPC UA
            foreach (var mapping in _configuration.TagMappings.Where(m => m.Enabled))
            {
                await _uaServer.AddOrUpdateNodeAsync(mapping);
                _logger.LogDebug("Mapped tag: {DaTag} -> {UaNode}", mapping.DaTagName, mapping.UaNodeName);
            }

            _logger.LogInformation("Subscribed to {Count} tags", enabledTags.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to tags");
        }
    }

    /// <summary>
    /// Handle OPC DA tag value changes - bridge to OPC UA
    /// </summary>
    private async void OnDaTagValueChanged(object? sender, TagValueChangedEventArgs e)
    {
        try
        {
            if (_uaServer == null || _configuration == null)
                return;

            // Find the mapping for this tag
            var mapping = _configuration.TagMappings.FirstOrDefault(m =>
                m.DaTagName == e.TagValue.TagName && m.Enabled);

            if (mapping == null)
                return;

            // Apply scaling if configured
            var value = e.TagValue.Value;
            if (value is double or float or int or long && mapping.ScaleFactor.HasValue)
            {
                var numericValue = Convert.ToDouble(value);
                numericValue = (numericValue * mapping.ScaleFactor.Value) + (mapping.Offset ?? 0);
                value = numericValue;
            }

            // Create UA value
            var uaValue = new TagValue
            {
                TagName = mapping.UaNodeName,
                Value = value,
                Quality = e.TagValue.Quality,
                Timestamp = e.TagValue.Timestamp,
                DataType = e.TagValue.DataType,
                Source = "DA->UA"
            };

            // Update OPC UA node
            await _uaServer.UpdateNodeValueAsync(mapping.UaNodeName, uaValue);

            _messagesProcessed++;

            _logger.LogDebug("Bridged DA->UA: {DaTag} = {Value} -> {UaNode}",
                e.TagValue.TagName, value, mapping.UaNodeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bridging DA->UA for tag {Tag}", e.TagValue.TagName);
            _status.ErrorCount++;
        }
    }

    /// <summary>
    /// Handle OPC UA node writes - bridge to OPC DA
    /// </summary>
    private async void OnUaNodeWritten(object? sender, NodeWriteEventArgs e)
    {
        try
        {
            if (_daClient == null || _configuration == null)
                return;

            // Find the reverse mapping
            var mapping = _configuration.TagMappings.FirstOrDefault(m =>
                m.UaNodeName == e.NodeName && m.Enabled &&
                m.AccessLevel != AccessLevel.Read);

            if (mapping == null)
                return;

            // Apply reverse scaling if configured
            var value = e.Value;
            if (value is double or float or int or long && mapping.ScaleFactor.HasValue && mapping.ScaleFactor.Value != 0)
            {
                var numericValue = Convert.ToDouble(value);
                numericValue = (numericValue - (mapping.Offset ?? 0)) / mapping.ScaleFactor.Value;
                value = numericValue;
            }

            // Write to OPC DA
            if (await _daClient.WriteTagAsync(mapping.DaTagName, value!))
            {
                _logger.LogInformation("Bridged UA->DA: {UaNode} = {Value} -> {DaTag}",
                    e.NodeName, value, mapping.DaTagName);
                _messagesProcessed++;
            }
            else
            {
                _logger.LogWarning("Failed to write to DA tag: {DaTag}", mapping.DaTagName);
                _status.ErrorCount++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bridging UA->DA for node {Node}", e.NodeName);
            _status.ErrorCount++;
        }
    }

    /// <summary>
    /// Handle OPC DA connection status changes
    /// </summary>
    private void OnDaConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
    {
        _status.DaConnectionStatus = e.Status;
        _logger.LogInformation("OPC DA connection status changed: {Status} - {Message}",
            e.Status, e.Message);
    }

    /// <summary>
    /// Handle OPC UA client connections
    /// </summary>
    private void OnClientConnected(object? sender, ClientConnectedEventArgs e)
    {
        _logger.LogInformation("OPC UA Client connected: {ClientName} ({ClientId})",
            e.ClientName ?? "Unknown", e.ClientId);
    }

    /// <summary>
    /// Handle OPC UA client disconnections
    /// </summary>
    private void OnClientDisconnected(object? sender, ClientDisconnectedEventArgs e)
    {
        _logger.LogInformation("OPC UA Client disconnected: {ClientId}", e.ClientId);
    }
}

