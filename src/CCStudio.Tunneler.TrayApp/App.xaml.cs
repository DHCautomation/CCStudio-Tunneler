using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using CCStudio.Tunneler.Core.Utilities;
using Serilog;

namespace CCStudio.Tunneler.TrayApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private ILogger? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize logger
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                System.IO.Path.Combine(Constants.DefaultLogPath, "tray-app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        _logger.Information("CCStudio-Tunneler Tray App starting - v{Version}", Constants.Version);
        _logger.Information("Developed by {Company}", Constants.CompanyName);

        // The tray icon is managed by MainWindow.xaml
        _logger.Information("Tray application initialized");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.Information("CCStudio-Tunneler Tray App exiting");
        base.OnExit(e);
    }
}
