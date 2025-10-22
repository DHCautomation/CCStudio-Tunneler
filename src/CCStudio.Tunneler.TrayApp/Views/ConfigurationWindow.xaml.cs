using System.Windows;
using System.Windows.Forms;
using CCStudio.Tunneler.Core.Models;
using CCStudio.Tunneler.Core.Services;
using CCStudio.Tunneler.Core.Utilities;
using CCStudio.Tunneler.TrayApp.Services;
using MessageBox = System.Windows.MessageBox;

namespace CCStudio.Tunneler.TrayApp.Views;

/// <summary>
/// Interaction logic for ConfigurationWindow.xaml
/// </summary>
public partial class ConfigurationWindow : Window
{
    private readonly ConfigurationService _configService;
    private TunnelerConfiguration _configuration;
    private List<OpcDaServerDiscovery.OpcServerInfo> _discoveredServers;

    public ConfigurationWindow()
    {
        InitializeComponent();
        _configService = new ConfigurationService();
        _configuration = new TunnelerConfiguration();
        _discoveredServers = new List<OpcDaServerDiscovery.OpcServerInfo>();
        LoadConfiguration();
        _ = DiscoverOpcServersAsync(); // Run discovery in background
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

    /// <summary>
    /// Discovers OPC DA servers on the local machine and populates the dropdown
    /// </summary>
    private async Task DiscoverOpcServersAsync()
    {
        try
        {
            // Show loading indicator (would need to add to UI)
            this.Cursor = System.Windows.Input.Cursors.Wait;

            await Task.Run(() =>
            {
                _discoveredServers = OpcDaServerDiscovery.DiscoverLocalServers();
            });

            // Populate ComboBox on UI thread
            Dispatcher.Invoke(() =>
            {
                CboServerProgId.Items.Clear();

                foreach (var server in _discoveredServers)
                {
                    var item = new System.Windows.Controls.ComboBoxItem
                    {
                        Content = server.ToString(),
                        Tag = server.ProgId
                    };
                    CboServerProgId.Items.Add(item);
                }

                // Add common servers that might not be discovered
                var commonProgIds = OpcDaServerDiscovery.GetCommonProgIds();
                foreach (var progId in commonProgIds)
                {
                    if (!_discoveredServers.Any(s => s.ProgId.Equals(progId, StringComparison.OrdinalIgnoreCase)))
                    {
                        var item = new System.Windows.Controls.ComboBoxItem
                        {
                            Content = $"{progId} (not detected)",
                            Tag = progId
                        };
                        CboServerProgId.Items.Add(item);
                    }
                }

                // If we had a ProgId configured, select it
                if (!string.IsNullOrEmpty(_configuration.OpcDa.ServerProgId))
                {
                    CboServerProgId.Text = _configuration.OpcDa.ServerProgId;
                }
            });
        }
        catch (Exception ex)
        {
            // Silent failure - user can still enter ProgID manually
            System.Diagnostics.Debug.WriteLine($"OPC server discovery failed: {ex.Message}");
        }
        finally
        {
            this.Cursor = System.Windows.Input.Cursors.Arrow;
        }
    }

    private async void OnTestDaConnection(object sender, RoutedEventArgs e)
    {
        var progId = CboServerProgId.Text;
        var host = TxtServerHost.Text;

        if (string.IsNullOrWhiteSpace(progId))
        {
            MessageBox.Show(
                "Please enter or select an OPC DA Server ProgID first.",
                "Missing Information",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Disable button during test
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Content = "Testing...";
            }

            this.Cursor = System.Windows.Input.Cursors.Wait;

            var (isAccessible, message) = await OpcDaServerDiscovery.TestServerConnection(progId, host);

            MessageBox.Show(
                message,
                isAccessible ? "Connection Successful" : "Connection Failed",
                MessageBoxButton.OK,
                isAccessible ? MessageBoxImage.Information : MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error testing connection: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            var button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                button.IsEnabled = true;
                button.Content = "Test Connection";
            }
            this.Cursor = System.Windows.Input.Cursors.Arrow;
        }
    }

    private void OnBrowseDaTags(object sender, RoutedEventArgs e)
    {
        // TODO: Implement tag browser - requires full COM interop implementation
        MessageBox.Show(
            "Tag browsing is available when AutoDiscoverTags is enabled.\n\n" +
            "The service will automatically discover and expose all available tags from the OPC DA server.\n\n" +
            "Manual tag mapping is optional and only needed for:\n" +
            "• Renaming tags\n" +
            "• Applying transformations\n" +
            "• Setting custom update rates per tag",
            "Auto Tag Discovery",
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
