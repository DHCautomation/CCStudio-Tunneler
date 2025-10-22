using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using CCStudio.Tunneler.Core.Utilities;
using CCStudio.Tunneler.TrayApp.Views;

namespace CCStudio.Tunneler.TrayApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// This is a hidden window that manages the system tray icon
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Hide the window immediately
        this.Hide();

        // Show balloon tip on startup
        TrayIcon.ShowBalloonTip(
            "CCStudio-Tunneler",
            "OPC DA to OPC UA Bridge is running in the system tray",
            Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);

        // Update service menu items based on current state
        UpdateServiceMenuItems();
    }

    private void OnTrayIconLeftClick(object sender, RoutedEventArgs e)
    {
        // Double-click opens configuration
        OnConfigure(sender, e);
    }

    private void OnConfigure(object sender, RoutedEventArgs e)
    {
        var configWindow = new ConfigurationWindow();
        configWindow.ShowDialog();
        UpdateServiceMenuItems();
    }

    private void OnViewStatus(object sender, RoutedEventArgs e)
    {
        var statusWindow = new StatusWindow();
        statusWindow.ShowDialog();
    }

    private void OnStartService(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = GetService();
            if (service != null && service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                TrayIcon.ShowBalloonTip(
                    "Service Started",
                    "CCStudio-Tunneler service has been started",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                UpdateServiceMenuItems();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start service: {ex.Message}\n\nMake sure you have administrator privileges.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnStopService(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = GetService();
            if (service != null && service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
                TrayIcon.ShowBalloonTip(
                    "Service Stopped",
                    "CCStudio-Tunneler service has been stopped",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Warning);
                UpdateServiceMenuItems();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to stop service: {ex.Message}\n\nMake sure you have administrator privileges.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnRestartService(object sender, RoutedEventArgs e)
    {
        try
        {
            var service = GetService();
            if (service != null)
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
                service.Start();
                TrayIcon.ShowBalloonTip(
                    "Service Restarted",
                    "CCStudio-Tunneler service has been restarted",
                    Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                UpdateServiceMenuItems();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to restart service: {ex.Message}\n\nMake sure you have administrator privileges.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnViewLogs(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(Constants.DefaultLogPath))
            {
                Process.Start("explorer.exe", Constants.DefaultLogPath);
            }
            else
            {
                MessageBox.Show(
                    $"Log directory not found: {Constants.DefaultLogPath}",
                    "Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open logs: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnOpenConfigFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            var configDir = Path.GetDirectoryName(Constants.DefaultConfigPath);
            if (!string.IsNullOrEmpty(configDir))
            {
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }
                Process.Start("explorer.exe", configDir);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open config folder: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.ShowDialog();
    }

    private void OnExit(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to exit?\n\nThe service will continue running in the background.",
            "Confirm Exit",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }

    private ServiceController? GetService()
    {
        try
        {
            return new ServiceController(Constants.ServiceName);
        }
        catch
        {
            return null;
        }
    }

    private void UpdateServiceMenuItems()
    {
        try
        {
            // Get the menu items from the context menu resource
            var contextMenu = (ContextMenu)this.FindResource("TrayContextMenu");
            var serviceMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Header.ToString() == "Service");

            if (serviceMenuItem != null)
            {
                var menuStartService = serviceMenuItem.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "MenuStartService");
                var menuStopService = serviceMenuItem.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "MenuStopService");
                var menuRestartService = serviceMenuItem.Items.OfType<MenuItem>().FirstOrDefault(m => m.Name == "MenuRestartService");

                var service = GetService();
                if (service != null)
                {
                    service.Refresh();
                    bool isRunning = service.Status == ServiceControllerStatus.Running;

                    if (menuStartService != null) menuStartService.IsEnabled = !isRunning;
                    if (menuStopService != null) menuStopService.IsEnabled = isRunning;
                    if (menuRestartService != null) menuRestartService.IsEnabled = isRunning;

                    // Update tray icon tooltip
                    TrayIcon.ToolTipText = $"CCStudio-Tunneler - {(isRunning ? "Running" : "Stopped")}";
                }
                else
                {
                    // Service not installed
                    if (menuStartService != null) menuStartService.IsEnabled = false;
                    if (menuStopService != null) menuStopService.IsEnabled = false;
                    if (menuRestartService != null) menuRestartService.IsEnabled = false;
                    TrayIcon.ToolTipText = "CCStudio-Tunneler - Service Not Installed";
                }
            }
        }
        catch
        {
            // Ignore errors when checking service status
        }
    }
}
