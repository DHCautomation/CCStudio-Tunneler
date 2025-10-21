using CCStudio.Tunneler.Core.Services;
using CCStudio.Tunneler.Core.Utilities;
using CCStudio.Tunneler.Service;
using Serilog;

// Create initial logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(Constants.DefaultLogPath, "ccstudio-tunneler-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("Starting {ServiceName} v{Version}", Constants.ApplicationName, Constants.Version);
    Log.Information("Developed by {Company}", Constants.CompanyName);

    // Ensure log directory exists
    LoggerFactory.EnsureLogDirectoryExists(Constants.DefaultLogPath);

    var builder = Host.CreateDefaultBuilder(args)
        .UseWindowsService(options =>
        {
            options.ServiceName = Constants.ServiceName;
        })
        .ConfigureServices(services =>
        {
            // Add configuration service
            services.AddSingleton<ConfigurationService>();

            // Add hosted service
            services.AddHostedService<TunnelerWorker>();
        })
        .UseSerilog();

    var host = builder.Build();

    Log.Information("Service configured successfully, starting host...");
    await host.RunAsync();

    Log.Information("Service stopped gracefully");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
