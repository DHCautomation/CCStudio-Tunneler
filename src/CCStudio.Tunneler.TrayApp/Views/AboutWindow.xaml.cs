using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CCStudio.Tunneler.TrayApp.Views;

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch
        {
            // Ignore navigation errors
        }
    }
}
