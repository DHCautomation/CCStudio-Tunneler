using CCStudio.Tunneler.Core.Models;
using CCStudio.Tunneler.Core.Services;
using CCStudio.Tunneler.Core.Utilities;

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

    // TODO: These will be implemented with actual OPC DA and UA services
    // private IOpcDaClient? _daClient;
    // private IOpcUaServer? _uaServer;

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

            // TODO: Initialize OPC DA Client
            _logger.LogInformation("Initializing OPC DA Client...");
            _logger.LogInformation("OPC DA Server: {ServerProgId} on {ServerHost}",
                _configuration.OpcDa.ServerProgId, _configuration.OpcDa.ServerHost);

            // For now, we'll simulate the connection
            await Task.Delay(1000, stoppingToken);
            _status.DaConnectionStatus = ConnectionStatus.Connected;
            _logger.LogInformation("OPC DA Client initialized (placeholder)");

            // TODO: Initialize OPC UA Server
            _logger.LogInformation("Initializing OPC UA Server...");
            _logger.LogInformation("OPC UA Endpoint: {EndpointUrl}", _configuration.OpcUa.EndpointUrl);

            // For now, we'll simulate the server start
            await Task.Delay(1000, stoppingToken);
            _status.UaServerStatus = ConnectionStatus.Connected;
            _logger.LogInformation("OPC UA Server started (placeholder)");

            _status.State = ServiceState.Running;
            _status.StartTime = _startTime;
            _logger.LogInformation("CCStudio-Tunneler is now running and ready to bridge OPC DA to OPC UA");

            // Main service loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Update status
                    _status.Uptime = _uptimeWatch.Elapsed;
                    _status.LastUpdate = DateTime.UtcNow;
                    _status.TotalMessagesProcessed = _messagesProcessed;

                    // TODO: Process tag updates
                    // This is where we'll bridge data between OPC DA and OPC UA

                    // Log periodic heartbeat
                    if (_messagesProcessed % 1000 == 0 && _messagesProcessed > 0)
                    {
                        _logger.LogInformation(
                            "Service heartbeat - Uptime: {Uptime}, Messages: {Messages}, Clients: {Clients}",
                            _status.Uptime.ToFriendlyString(),
                            _messagesProcessed,
                            _status.ConnectedClients);
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
            // TODO: Stop OPC UA Server
            _logger.LogInformation("Stopping OPC UA Server...");
            _status.UaServerStatus = ConnectionStatus.Disconnected;

            // TODO: Disconnect OPC DA Client
            _logger.LogInformation("Disconnecting OPC DA Client...");
            _status.DaConnectionStatus = ConnectionStatus.Disconnected;

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
}
