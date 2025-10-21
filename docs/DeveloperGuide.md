# CCStudio-Tunneler Developer Guide

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Development Setup](#development-setup)
4. [Building the Project](#building-the-project)
5. [Project Structure](#project-structure)
6. [Key Components](#key-components)
7. [Adding New Features](#adding-new-features)
8. [Testing](#testing)
9. [Deployment](#deployment)

## Project Overview

CCStudio-Tunneler is a .NET 8.0 LTS application that bridges OPC DA to OPC UA. The project consists of three main components:

- **CCStudio.Tunneler.Core**: Shared library with models, interfaces, and services
- **CCStudio.Tunneler.Service**: Windows service that handles OPC communication
- **CCStudio.Tunneler.TrayApp**: WPF application for configuration and monitoring

### Technology Stack

- **.NET 8.0 LTS**: Target framework (supported until November 2026)
- **C# 12**: Programming language
- **WPF**: UI framework
- **Serilog**: Logging
- **Newtonsoft.Json**: Configuration serialization
- **Material Design**: UI theme
- **OPC Foundation UA .NET Standard**: OPC UA implementation (open-source)
- **OPC DA .NET**: OPC DA client wrapper

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────┐
│   CCStudio.Tunneler.TrayApp (WPF)  │
│   - Configuration UI                │
│   - Service Control                 │
│   - Status Monitoring               │
└──────────────┬──────────────────────┘
               │ IPC / Service Control
┌──────────────┴──────────────────────┐
│   CCStudio.Tunneler.Service         │
│   ┌──────────────────────────────┐  │
│   │ TunnelerWorker               │  │
│   │  - Coordinates components    │  │
│   │  - Manages lifecycle         │  │
│   └──────┬───────────────┬────────┘  │
│          │               │           │
│   ┌──────┴─────┐  ┌──────┴─────┐   │
│   │ OPC DA     │  │ OPC UA     │   │
│   │ Client     │  │ Server     │   │
│   └──────┬─────┘  └──────┬─────┘   │
└──────────┼────────────────┼─────────┘
           │                │
      ┌────┴────┐    ┌──────┴──────┐
      │ OPC DA  │    │ OPC UA      │
      │ Server  │    │ Clients     │
      └─────────┘    └─────────────┘
```

### Component Interaction

1. **Configuration Flow**:
   - User configures via TrayApp
   - Configuration saved to JSON file
   - Service loads configuration on startup/restart

2. **Data Flow**:
   - OPC DA Client subscribes to tags
   - Tag changes trigger events
   - Bridge logic processes and maps data
   - OPC UA Server updates node values
   - OPC UA clients receive notifications

3. **Service Control**:
   - TrayApp controls service via Windows Service API
   - Service status monitored via status queries
   - Logs written to shared directory

## Development Setup

### Prerequisites

1. **Visual Studio 2022** (recommended) or Visual Studio Code
2. **.NET 8.0 SDK** (download: https://dotnet.microsoft.com/download/dotnet/8.0)
3. **Git** for version control
4. **OPC Core Components 3.0** for OPC DA testing

### Clone Repository

```bash
git clone https://github.com/yourusername/CCStudio-Tunneler.git
cd CCStudio-Tunneler
```

### Restore NuGet Packages

```bash
dotnet restore
```

### Open in Visual Studio

1. Open `CCStudio-Tunneler.sln`
2. Set `CCStudio.Tunneler.TrayApp` as startup project for UI development
3. Set `CCStudio.Tunneler.Service` as startup project for service development

## Building the Project

### Build from Command Line

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release
```

### Build from Visual Studio

1. Select configuration (Debug/Release)
2. Build → Build Solution (Ctrl+Shift+B)

### Output Directories

- Debug: `src/[ProjectName]/bin/Debug/net8.0-windows/`
- Release: `src/[ProjectName]/bin/Release/net8.0-windows/`

## Project Structure

```
CCStudio-Tunneler/
├── src/
│   ├── CCStudio.Tunneler.Core/
│   │   ├── Models/              # Data models
│   │   ├── Interfaces/          # Service interfaces
│   │   ├── Services/            # Implementations
│   │   └── Utilities/           # Helper classes
│   ├── CCStudio.Tunneler.Service/
│   │   ├── Program.cs           # Service entry point
│   │   ├── TunnelerWorker.cs    # Main service logic
│   │   └── appsettings.json     # Service configuration
│   └── CCStudio.Tunneler.TrayApp/
│       ├── App.xaml             # WPF application
│       ├── MainWindow.xaml      # Tray icon manager
│       └── Views/               # UI windows
│           ├── ConfigurationWindow.xaml
│           ├── StatusWindow.xaml
│           └── AboutWindow.xaml
├── tests/
│   └── CCStudio.Tunneler.Tests/ # Unit tests
├── docs/
│   ├── UserGuide.md
│   └── DeveloperGuide.md
├── images/                      # Branding assets
├── LICENSE
├── README.md
└── CCStudio-Tunneler.sln
```

## Key Components

### Core Library (CCStudio.Tunneler.Core)

#### Models

**TunnelerConfiguration**: Main configuration container
```csharp
public class TunnelerConfiguration
{
    public OpcDaConfiguration OpcDa { get; set; }
    public OpcUaConfiguration OpcUa { get; set; }
    public LoggingConfiguration Logging { get; set; }
    public List<TagMapping> TagMappings { get; set; }
}
```

**TagMapping**: Maps OPC DA tags to OPC UA nodes
```csharp
public class TagMapping
{
    public string DaTagName { get; set; }
    public string UaNodeName { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public double? ScaleFactor { get; set; }
    public double? Offset { get; set; }
}
```

#### Interfaces

**IOpcDaClient**: OPC DA client operations
```csharp
public interface IOpcDaClient : IDisposable
{
    Task<bool> ConnectAsync(OpcDaConfiguration configuration);
    Task<TagValue?> ReadTagAsync(string tagName);
    Task<bool> WriteTagAsync(string tagName, object value);
    event EventHandler<TagValueChangedEventArgs>? TagValueChanged;
}
```

**IOpcUaServer**: OPC UA server operations
```csharp
public interface IOpcUaServer : IDisposable
{
    Task<bool> StartAsync(OpcUaConfiguration configuration);
    Task<bool> AddOrUpdateNodeAsync(TagMapping mapping, TagValue? initialValue);
    Task<bool> UpdateNodeValueAsync(string nodeName, TagValue value);
    event EventHandler<NodeWriteEventArgs>? NodeWritten;
}
```

#### Services

**ConfigurationService**: Manages configuration
```csharp
public class ConfigurationService : IConfigurationService
{
    public async Task<TunnelerConfiguration> LoadConfigurationAsync();
    public async Task<bool> SaveConfigurationAsync(TunnelerConfiguration config);
    public Task<(bool isValid, List<string> errors)> ValidateConfigurationAsync();
}
```

### Service (CCStudio.Tunneler.Service)

**TunnelerWorker**: Main service worker
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // 1. Load configuration
    _configuration = await _configService.LoadConfigurationAsync();

    // 2. Initialize OPC DA Client
    _daClient = new OpcDaClient();
    await _daClient.ConnectAsync(_configuration.OpcDa);

    // 3. Initialize OPC UA Server
    _uaServer = new OpcUaServer();
    await _uaServer.StartAsync(_configuration.OpcUa);

    // 4. Set up tag mappings
    foreach (var mapping in _configuration.TagMappings)
    {
        await _uaServer.AddOrUpdateNodeAsync(mapping);
    }

    // 5. Bridge data flow
    _daClient.TagValueChanged += OnDaTagChanged;
    _uaServer.NodeWritten += OnUaNodeWritten;

    // 6. Main loop
    while (!stoppingToken.IsCancellationRequested)
    {
        // Monitor and maintain connections
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
    }
}
```

### Tray Application (CCStudio.Tunneler.TrayApp)

**ConfigurationWindow**: User configuration interface
- Tabbed interface for OPC DA, OPC UA, Tag Mapping, Logging
- Validation before saving
- Import/Export functionality

**StatusWindow**: Real-time monitoring
- Service state display
- Connection status indicators
- Performance metrics
- Error reporting

## Adding New Features

### Adding a New Configuration Option

1. **Update Model** (`Core/Models/TunnelerConfiguration.cs`):
```csharp
public class OpcDaConfiguration
{
    // ... existing properties

    public int NewOption { get; set; } = defaultValue;
}
```

2. **Update UI** (`TrayApp/Views/ConfigurationWindow.xaml`):
```xml
<TextBlock Text="New Option" Margin="0,15,0,5"/>
<TextBox Name="TxtNewOption" Height="35"/>
```

3. **Update Code-Behind** (`ConfigurationWindow.xaml.cs`):
```csharp
private void PopulateFields()
{
    // ...
    TxtNewOption.Text = _configuration.OpcDa.NewOption.ToString();
}

private bool GatherConfiguration()
{
    // ...
    _configuration.OpcDa.NewOption = int.Parse(TxtNewOption.Text);
}
```

4. **Update Validation** (`Core/Services/ConfigurationService.cs`):
```csharp
public Task<(bool isValid, List<string> errors)> ValidateConfigurationAsync()
{
    // ...
    if (configuration.OpcDa.NewOption < minValue)
    {
        errors.Add("New option must be >= minimum");
    }
}
```

### Implementing OPC DA Client

**Note**: This is a placeholder for actual OPC DA implementation

```csharp
public class OpcDaClient : IOpcDaClient
{
    private OpcCom.Da.Server? _server;

    public async Task<bool> ConnectAsync(OpcDaConfiguration configuration)
    {
        try
        {
            var url = new OpcCom.Factory.URL(
                $"opcda://{configuration.ServerHost}/{configuration.ServerProgId}");

            _server = new OpcCom.Da.Server(new OpcCom.Factory(), url);
            _server.Connect();

            return _server.IsConnected;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to connect to OPC DA server");
            return false;
        }
    }

    public async Task<bool> SubscribeToTagsAsync(IEnumerable<string> tagNames)
    {
        var subscription = _server.CreateSubscription();
        subscription.DataChanged += OnDataChanged;

        foreach (var tag in tagNames)
        {
            subscription.AddItem(new Item { ItemName = tag });
        }

        return true;
    }

    private void OnDataChanged(object sender, DataChangedEventArgs e)
    {
        foreach (var value in e.Values)
        {
            TagValueChanged?.Invoke(this, new TagValueChangedEventArgs
            {
                TagValue = new TagValue
                {
                    TagName = value.ItemName,
                    Value = value.Value,
                    Quality = MapQuality(value.Quality),
                    Timestamp = value.Timestamp
                }
            });
        }
    }
}
```

### Implementing OPC UA Server

**Note**: This uses OPC Foundation UA .NET Standard

```csharp
public class OpcUaServer : IOpcUaServer
{
    private ApplicationInstance? _application;
    private StandardServer? _server;

    public async Task<bool> StartAsync(OpcUaConfiguration configuration)
    {
        _application = new ApplicationInstance
        {
            ApplicationName = configuration.ApplicationName,
            ApplicationType = ApplicationType.Server
        };

        await _application.LoadApplicationConfiguration(silent: false);

        _server = new StandardServer();
        await _application.Start(_server);

        return true;
    }

    public async Task<bool> AddOrUpdateNodeAsync(TagMapping mapping, TagValue? initialValue)
    {
        var node = new BaseDataVariableState(null)
        {
            NodeId = new NodeId(mapping.UaNodeName, mapping.NamespaceIndex),
            BrowseName = new QualifiedName(mapping.UaNodeName),
            DisplayName = mapping.UaNodeName,
            Value = initialValue?.Value,
            Timestamp = DateTime.UtcNow,
            AccessLevel = MapAccessLevel(mapping.AccessLevel),
            UserAccessLevel = MapAccessLevel(mapping.AccessLevel)
        };

        node.OnWriteValue = OnNodeWrite;
        _server.AddNode(node);

        return true;
    }
}
```

## Testing

### Unit Tests

Create tests in `tests/CCStudio.Tunneler.Tests/`:

```csharp
[TestClass]
public class ConfigurationServiceTests
{
    [TestMethod]
    public async Task LoadConfiguration_ValidFile_ReturnsConfiguration()
    {
        // Arrange
        var service = new ConfigurationService();

        // Act
        var config = await service.LoadConfigurationAsync();

        // Assert
        Assert.IsNotNull(config);
        Assert.IsNotNull(config.OpcDa);
        Assert.IsNotNull(config.OpcUa);
    }

    [TestMethod]
    public async Task ValidateConfiguration_InvalidPort_ReturnsErrors()
    {
        // Arrange
        var service = new ConfigurationService();
        var config = new TunnelerConfiguration();
        config.OpcUa.ServerPort = 99999; // Invalid

        // Act
        var (isValid, errors) = await service.ValidateConfigurationAsync(config);

        // Assert
        Assert.IsFalse(isValid);
        Assert.IsTrue(errors.Count > 0);
    }
}
```

### Integration Testing

1. Set up OPC DA simulator (Matrikon OPC Simulation Server)
2. Run service in debug mode
3. Connect OPC UA client (UAExpert)
4. Verify bidirectional data flow

## Deployment

### Creating Release Build

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

### Installing as Windows Service

```powershell
# Create service
sc create CCStudioTunneler binPath= "C:\Path\To\CCStudio.Tunneler.Service.exe" start= auto

# Start service
sc start CCStudioTunneler

# Check status
sc query CCStudioTunneler

# Stop service
sc stop CCStudioTunneler

# Delete service
sc delete CCStudioTunneler
```

### Creating MSI Installer (WiX)

1. Install WiX Toolset
2. Create installer project
3. Build installer

```bash
candle Product.wxs
light -out CCStudio-Tunneler-Setup.msi Product.wixobj
```

---

**For development questions, contact: dev@dhcautomation.com**
