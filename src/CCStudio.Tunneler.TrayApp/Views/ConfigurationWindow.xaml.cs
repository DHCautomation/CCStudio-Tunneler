using System.Windows;
using System.Windows.Forms;
using CCStudio.Tunneler.Core.Models;
using CCStudio.Tunneler.Core.Services;
using CCStudio.Tunneler.Core.Utilities;
using MessageBox = System.Windows.MessageBox;

namespace CCStudio.Tunneler.TrayApp.Views;

/// <summary>
/// Interaction logic for ConfigurationWindow.xaml
/// </summary>
public partial class ConfigurationWindow : Window
{
    private readonly ConfigurationService _configService;
    private TunnelerConfiguration _configuration;

    public ConfigurationWindow()
    {
        InitializeComponent();
        _configService = new ConfigurationService();
        _configuration = new TunnelerConfiguration();
        LoadConfiguration();
    }

    private async void LoadConfiguration()
    {
        try
        {
            _configuration = await _configService.LoadConfigurationAsync();
            PopulateFields();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load configuration: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PopulateFields()
    {
        // OPC DA
        CboServerProgId.Text = _configuration.OpcDa.ServerProgId;
        TxtServerHost.Text = _configuration.OpcDa.ServerHost;
        TxtUpdateRate.Text = _configuration.OpcDa.UpdateRate.ToString();
        TxtDeadBand.Text = _configuration.OpcDa.DeadBand.ToString();
        ChkAutoDiscover.IsChecked = _configuration.OpcDa.AutoDiscoverTags;

        // OPC UA
        TxtOpcUaPort.Text = _configuration.OpcUa.ServerPort.ToString();
        TxtServerName.Text = _configuration.OpcUa.ServerName;
        TxtEndpointUrl.Text = _configuration.OpcUa.EndpointUrl;
        CboSecurityMode.SelectedIndex = (int)_configuration.OpcUa.SecurityMode;
        ChkAllowAnonymous.IsChecked = _configuration.OpcUa.AllowAnonymous;
        TxtUsername.Text = _configuration.OpcUa.Username ?? string.Empty;

        // Tag Mappings
        DgTagMappings.ItemsSource = _configuration.TagMappings;

        // Logging
        CboLogLevel.SelectedItem = _configuration.Logging.Level;
        TxtLogPath.Text = _configuration.Logging.Path;
        TxtRetentionDays.Text = _configuration.Logging.RetentionDays.ToString();
        TxtMaxFileSize.Text = _configuration.Logging.MaxFileSizeMB.ToString();
        ChkEnableConsole.IsChecked = _configuration.Logging.EnableConsole;
    }

    private bool GatherConfiguration()
    {
        try
        {
            // OPC DA
            _configuration.OpcDa.ServerProgId = CboServerProgId.Text;
            _configuration.OpcDa.ServerHost = TxtServerHost.Text;
            _configuration.OpcDa.UpdateRate = int.Parse(TxtUpdateRate.Text);
            _configuration.OpcDa.DeadBand = float.Parse(TxtDeadBand.Text);
            _configuration.OpcDa.AutoDiscoverTags = ChkAutoDiscover.IsChecked ?? true;

            // OPC UA
            _configuration.OpcUa.ServerPort = int.Parse(TxtOpcUaPort.Text);
            _configuration.OpcUa.ServerName = TxtServerName.Text;
            _configuration.OpcUa.EndpointUrl = TxtEndpointUrl.Text;
            _configuration.OpcUa.SecurityMode = (SecurityMode)CboSecurityMode.SelectedIndex;
            _configuration.OpcUa.AllowAnonymous = ChkAllowAnonymous.IsChecked ?? true;
            _configuration.OpcUa.Username = TxtUsername.Text;

            // Logging
            _configuration.Logging.Level = (CboLogLevel.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Information";
            _configuration.Logging.Path = TxtLogPath.Text;
            _configuration.Logging.RetentionDays = int.Parse(TxtRetentionDays.Text);
            _configuration.Logging.MaxFileSizeMB = int.Parse(TxtMaxFileSize.Text);
            _configuration.Logging.EnableConsole = ChkEnableConsole.IsChecked ?? true;

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Invalid configuration: {ex.Message}",
                "Validation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        if (!GatherConfiguration())
            return;

        if (await SaveConfiguration())
        {
            DialogResult = true;
            Close();
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (!GatherConfiguration())
            return;

        await SaveConfiguration();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async Task<bool> SaveConfiguration()
    {
        try
        {
            var (isValid, errors) = await _configService.ValidateConfigurationAsync(_configuration);
            if (!isValid)
            {
                MessageBox.Show(
                    $"Configuration validation failed:\n\n{string.Join("\n", errors)}",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (await _configService.SaveConfigurationAsync(_configuration))
            {
                MessageBox.Show(
                    "Configuration saved successfully!\n\nRestart the service for changes to take effect.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return true;
            }
            else
            {
                MessageBox.Show(
                    "Failed to save configuration",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error saving configuration: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void OnTestDaConnection(object sender, RoutedEventArgs e)
    {
        // TODO: Implement OPC DA connection test
        MessageBox.Show(
            "OPC DA connection test will be implemented with the OPC DA client library.",
            "Not Implemented",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnBrowseDaTags(object sender, RoutedEventArgs e)
    {
        // TODO: Implement tag browser
        MessageBox.Show(
            "Tag browser will be implemented with the OPC DA client library.",
            "Not Implemented",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnImportMappings(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Import Tag Mappings"
        };

        if (dialog.ShowDialog() == true)
        {
            // TODO: Implement import
            MessageBox.Show("Import functionality coming soon", "Not Implemented",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnExportMappings(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            Title = "Export Tag Mappings",
            FileName = "tag-mappings.json"
        };

        if (dialog.ShowDialog() == true)
        {
            // TODO: Implement export
            MessageBox.Show("Export functionality coming soon", "Not Implemented",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnBrowseLogPath(object sender, RoutedEventArgs e)
    {
        var dialog = new FolderBrowserDialog
        {
            Description = "Select log directory",
            SelectedPath = TxtLogPath.Text
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtLogPath.Text = dialog.SelectedPath;
        }
    }
}
