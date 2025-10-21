using System.Windows;
using System.Windows.Media;
using CCStudio.Tunneler.Core.Models;

namespace CCStudio.Tunneler.TrayApp.Views;

/// <summary>
/// Interaction logic for StatusWindow.xaml
/// </summary>
public partial class StatusWindow : Window
{
    public StatusWindow()
    {
        InitializeComponent();
        Loaded += StatusWindow_Loaded;
    }

    private void StatusWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        // TODO: Get actual status from service
        // For now, show placeholder data

        var status = new TunnelerStatus
        {
            State = ServiceState.Running,
            DaConnectionStatus = ConnectionStatus.Connected,
            UaServerStatus = ConnectionStatus.Connected,
            ActiveTagCount = 0,
            ConnectedClients = 0,
            TotalMessagesProcessed = 0,
            MessagesPerSecond = 0.0,
            ErrorCount = 0,
            LastError = null,
            Uptime = TimeSpan.Zero,
            StartTime = DateTime.Now
        };

        UpdateUI(status);
    }

    private void UpdateUI(TunnelerStatus status)
    {
        // Service State
        TxtServiceState.Text = status.State.ToString();
        TxtServiceState.Foreground = status.State == ServiceState.Running
            ? Brushes.Green
            : status.State == ServiceState.Error
                ? Brushes.Red
                : Brushes.Orange;

        TxtUptime.Text = $"Uptime: {FormatTimeSpan(status.Uptime)}";

        // Connection Status
        UpdateConnectionStatus(DaStatusIndicator, TxtDaStatus, status.DaConnectionStatus);
        UpdateConnectionStatus(UaStatusIndicator, TxtUaStatus, status.UaServerStatus);

        // Statistics
        TxtActiveTag.Text = status.ActiveTagCount.ToString();
        TxtConnectedClients.Text = status.ConnectedClients.ToString();
        TxtMessagesProcessed.Text = status.TotalMessagesProcessed.ToString("N0");
        TxtMessagesPerSecond.Text = status.MessagesPerSecond.ToString("F2");
        TxtErrorCount.Text = status.ErrorCount.ToString();

        // Last Error
        if (!string.IsNullOrEmpty(status.LastError))
        {
            CardLastError.Visibility = Visibility.Visible;
            TxtLastError.Text = status.LastError;
        }
        else
        {
            CardLastError.Visibility = Visibility.Collapsed;
        }

        // Last Update
        TxtLastUpdate.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
    }

    private void UpdateConnectionStatus(System.Windows.Shapes.Ellipse indicator, System.Windows.Controls.TextBlock text, ConnectionStatus status)
    {
        switch (status)
        {
            case ConnectionStatus.Connected:
                indicator.Fill = Brushes.Green;
                text.Text = "Connected";
                break;
            case ConnectionStatus.Connecting:
            case ConnectionStatus.Reconnecting:
                indicator.Fill = Brushes.Orange;
                text.Text = status.ToString();
                break;
            case ConnectionStatus.Disconnected:
                indicator.Fill = Brushes.Gray;
                text.Text = "Disconnected";
                break;
            case ConnectionStatus.Error:
                indicator.Fill = Brushes.Red;
                text.Text = "Error";
                break;
        }
    }

    private string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
            return $"{timeSpan.Seconds}s";

        if (timeSpan.TotalMinutes < 60)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";

        if (timeSpan.TotalHours < 24)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";

        return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
    }

    private void OnRefresh(object sender, RoutedEventArgs e)
    {
        RefreshStatus();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
