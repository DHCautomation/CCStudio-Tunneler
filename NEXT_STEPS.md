# Next Steps to Complete CCStudio-Tunneler

This document outlines the remaining work to make CCStudio-Tunneler fully functional.

## Current Status

### ✅ Completed
- [x] Project structure and solution setup
- [x] Core library with models, interfaces, and utilities
- [x] Configuration management with JSON serialization
- [x] Windows Service project structure
- [x] WPF System Tray application with full UI
- [x] Configuration window with all settings tabs
- [x] Status monitoring window
- [x] About window
- [x] Service control integration
- [x] Logging infrastructure
- [x] Comprehensive user and developer documentation

### 🔨 In Progress / Pending

## 1. Create Application Icon (.ico file)

**Status**: Pending
**Priority**: High
**Time Estimate**: 15 minutes

### Steps:

1. **Convert PNG to ICO using online tool**:
   - Visit: https://convertio.co/png-ico/
   - Upload `images/CCStudio-Tunneler.png`
   - Download as `CCStudio-Tunneler.ico`
   - Save to `images/` folder

2. **Or use ImageMagick**:
   ```bash
   magick convert images/CCStudio-Tunneler.png -define icon:auto-resize=256,128,64,48,32,16 images/CCStudio-Tunneler.ico
   ```

3. **Or use GIMP**:
   - Open PNG in GIMP
   - File → Export As
   - Choose .ico format
   - Select multiple sizes (16x16, 32x32, 48x48, 256x256)
   - Export

4. **Update Project References**:
   The .csproj files already reference the icon, just needs the file to exist

## 2. Implement OPC DA Client

**Status**: Not Started
**Priority**: Critical
**Time Estimate**: 2-3 days

### Library Options:

#### Option A: OPC Foundation .NET API (Free, but Complex)
- Download: https://opcfoundation.org/developer-tools/specifications-classic
- Requires COM interop with OPC Core Components
- Well-documented but requires DCOM configuration

#### Option B: Technosoftware DaNetStdLib (Commercial, ~$500-1000)
- Website: https://technosoftware.com/
- Modern .NET Standard library
- Easier to use, better documentation
- Recommended for production use

#### Option C: Open-Source Alternatives
- **OpcNetApi** - https://github.com/OPCFoundation/UA-.NET-Legacy
- **Hylasoft.Opc** - Basic functionality
- Limited features, may need enhancements

### Implementation Guide:

1. **Choose and install library** (recommend Technosoftware for commercial use)

2. **Create implementation** at `src/CCStudio.Tunneler.Service/OPC/OpcDaClient.cs`:

```csharp
using CCStudio.Tunneler.Core.Interfaces;
using CCStudio.Tunneler.Core.Models;
using Technosoftware.DaNetStdLib; // Example

public class OpcDaClient : IOpcDaClient
{
    private OpcServer? _server;
    private OpcGroup? _group;

    public async Task<bool> ConnectAsync(OpcDaConfiguration configuration)
    {
        _server = new OpcServer();
        var result = _server.Connect(configuration.ServerProgId, configuration.ServerHost);

        if (result.IsSuccess)
        {
            _group = _server.AddGroup("CCStudioGroup", configuration.UpdateRate);
            _group.DataChange += OnDataChange;
            return true;
        }
        return false;
    }

    public async Task<bool> SubscribeToTagsAsync(IEnumerable<string> tagNames)
    {
        var items = tagNames.Select(tag => new OpcItem(tag)).ToArray();
        _group.AddItems(items);
        return true;
    }

    // Implement remaining interface methods...
}
```

3. **Integrate into TunnelerWorker.cs**:
```csharp
_daClient = new OpcDaClient(_logger);
await _daClient.ConnectAsync(_configuration.OpcDa);
_daClient.TagValueChanged += OnDaTagChanged;
```

## 3. Implement OPC UA Server

**Status**: Not Started
**Priority**: Critical
**Time Estimate**: 2-3 days

### Use OPC Foundation UA .NET Standard (Free, Open-Source)

Already included in Service project NuGet packages.

### Implementation Guide:

1. **Create implementation** at `src/CCStudio.Tunneler.Service/OPC/OpcUaServer.cs`:

```csharp
using Opc.Ua;
using Opc.Ua.Server;
using CCStudio.Tunneler.Core.Interfaces;

public class OpcUaServer : IOpcUaServer
{
    private ApplicationInstance? _application;
    private StandardServer? _server;
    private Dictionary<string, BaseDataVariableState> _nodes = new();

    public async Task<bool> StartAsync(OpcUaConfiguration configuration)
    {
        _application = new ApplicationInstance
        {
            ApplicationName = configuration.ApplicationName,
            ApplicationType = ApplicationType.Server,
            ConfigSectionName = "CCStudioTunneler"
        };

        // Load application configuration
        var config = await _application.LoadApplicationConfiguration(silent: false);

        // Check certificate
        bool certOk = await _application.CheckApplicationInstanceCertificate(
            silent: false, minimumKeySize: 0);

        // Start server
        _server = new TunnelerServer();
        await _application.Start(_server);

        return true;
    }

    public async Task<bool> AddOrUpdateNodeAsync(TagMapping mapping, TagValue? initialValue)
    {
        var node = new BaseDataVariableState(null)
        {
            NodeId = new NodeId(mapping.UaNodeName, 2),
            BrowseName = new QualifiedName(mapping.UaNodeName),
            DisplayName = new LocalizedText(mapping.UaNodeName),
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None,
            Value = initialValue?.Value,
            StatusCode = StatusCodes.Good,
            Timestamp = DateTime.UtcNow,
            AccessLevel = (byte)AccessLevels.CurrentReadOrWrite,
            UserAccessLevel = (byte)AccessLevels.CurrentReadOrWrite,
            Historizing = false
        };

        node.OnWriteValue = OnNodeWriteValue;
        _nodes[mapping.UaNodeName] = node;
        _server.AddNode(node);

        return true;
    }

    // Implement remaining methods...
}
```

2. **Create OPC UA Application Configuration** at `src/CCStudio.Tunneler.Service/OpcUaConfig.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <ApplicationName>CCStudio-Tunneler</ApplicationName>
  <ApplicationUri>urn:DHCAutomation:CCStudio-Tunneler</ApplicationUri>
  <ProductUri>https://dhcautomation.com/ccstudio-tunneler</ProductUri>
  <ApplicationType>Server_0</ApplicationType>

  <ServerConfiguration>
    <BaseAddresses>
      <BaseAddress>opc.tcp://localhost:4840</BaseAddress>
    </BaseAddresses>
    <SecurityPolicies>
      <ServerSecurityPolicy>
        <SecurityMode>None_1</SecurityMode>
        <SecurityPolicyUri>http://opcfoundation.org/UA/SecurityPolicy#None</SecurityPolicyUri>
      </ServerSecurityPolicy>
    </SecurityPolicies>
  </ServerConfiguration>

  <TransportConfigurations />
  <TransportQuotas>
    <OperationTimeout>600000</OperationTimeout>
    <MaxStringLength>1048576</MaxStringLength>
    <MaxByteStringLength>1048576</MaxByteStringLength>
  </TransportQuotas>
</ApplicationConfiguration>
```

## 4. Implement Bridge Logic

**Status**: Not Started
**Priority**: High
**Time Estimate**: 1 day

### In `TunnelerWorker.cs`:

```csharp
private void OnDaTagChanged(object? sender, TagValueChangedEventArgs e)
{
    try
    {
        // Find mapping
        var mapping = _configuration.TagMappings
            .FirstOrDefault(m => m.DaTagName == e.TagValue.TagName && m.Enabled);

        if (mapping == null)
            return;

        // Apply scaling if configured
        var value = e.TagValue.Value;
        if (value is double numericValue && mapping.ScaleFactor.HasValue)
        {
            numericValue = (numericValue * mapping.ScaleFactor.Value) + (mapping.Offset ?? 0);
            value = numericValue;
        }

        // Update OPC UA node
        var uaValue = new TagValue
        {
            TagName = mapping.UaNodeName,
            Value = value,
            Quality = e.TagValue.Quality,
            Timestamp = e.TagValue.Timestamp
        };

        await _uaServer.UpdateNodeValueAsync(mapping.UaNodeName, uaValue);

        _messagesProcessed++;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error bridging DA to UA for tag {Tag}", e.TagValue.TagName);
        _status.ErrorCount++;
    }
}

private async void OnUaNodeWritten(object? sender, NodeWriteEventArgs e)
{
    try
    {
        // Find reverse mapping
        var mapping = _configuration.TagMappings
            .FirstOrDefault(m => m.UaNodeName == e.NodeName && m.Enabled
                && m.AccessLevel != AccessLevel.Read);

        if (mapping == null)
            return;

        // Apply reverse scaling if configured
        var value = e.Value;
        if (value is double numericValue && mapping.ScaleFactor.HasValue)
        {
            numericValue = (numericValue - (mapping.Offset ?? 0)) / mapping.ScaleFactor.Value;
            value = numericValue;
        }

        // Write to OPC DA
        await _daClient.WriteTagAsync(mapping.DaTagName, value);

        _logger.LogInformation("Write from UA to DA: {UANode} → {DATag} = {Value}",
            e.NodeName, mapping.DaTagName, value);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error bridging UA to DA for node {Node}", e.NodeName);
        _status.ErrorCount++;
    }
}
```

## 5. Implement Tag Browser

**Status**: Not Started
**Priority**: Medium
**Time Estimate**: 1 day

### In `ConfigurationWindow.xaml.cs`:

```csharp
private async void OnBrowseDaTags(object sender, RoutedEventArgs e)
{
    try
    {
        var browser = new TagBrowserWindow();

        // Create temporary DA client for browsing
        using var client = new OpcDaClient();
        var config = new OpcDaConfiguration
        {
            ServerProgId = CboServerProgId.Text,
            ServerHost = TxtServerHost.Text
        };

        if (await client.ConnectAsync(config))
        {
            var tags = await client.BrowseTagsAsync();
            browser.PopulateTags(tags);

            if (browser.ShowDialog() == true)
            {
                // Add selected tags to mapping grid
                foreach (var tag in browser.SelectedTags)
                {
                    _configuration.TagMappings.Add(new TagMapping
                    {
                        DaTagName = tag,
                        UaNodeName = tag.Replace('.', '/'),
                        Enabled = true,
                        AccessLevel = AccessLevel.ReadWrite
                    });
                }
                DgTagMappings.Items.Refresh();
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error browsing tags: {ex.Message}", "Error");
    }
}
```

Create `TagBrowserWindow.xaml` with TreeView for hierarchical browsing.

## 6. Create MSI Installer with WiX

**Status**: Not Started
**Priority**: Medium
**Time Estimate**: 1 day

### Install WiX Toolset:
- Download: https://wixtoolset.org/releases/
- Or: `dotnet tool install --global wix`

### Create Installer Project:

See `BUILD.md` for detailed WiX setup.

## 7. Implement IPC for Tray-Service Communication

**Status**: Not Started (currently using Windows Service API only)
**Priority**: Low (current solution works)
**Time Estimate**: 1 day

### Optional Enhancement:

Use Named Pipes for real-time status updates instead of polling service.

```csharp
// Server (in Service)
var pipe = new NamedPipeServerStream("CCStudioTunnelerPipe");
await pipe.WaitForConnectionAsync();

// Client (in TrayApp)
var pipe = new NamedPipeClientStream(".", "CCStudioTunnelerPipe");
await pipe.ConnectAsync();
```

## 8. Testing Checklist

- [ ] Build solution without errors
- [ ] Install service on test machine
- [ ] Connect to OPC DA simulator (Matrikon)
- [ ] Configure OPC UA endpoint
- [ ] Test with OPC UA client (UAExpert)
- [ ] Verify bidirectional data flow
- [ ] Test automatic reconnection
- [ ] Test tag mapping and scaling
- [ ] Verify logging
- [ ] Test service start/stop/restart
- [ ] Test on clean Windows install
- [ ] Performance test with 100+ tags

## 9. Pre-Release Tasks

- [ ] Code review
- [ ] Update version numbers
- [ ] Finalize CHANGELOG
- [ ] Create release notes
- [ ] Build release packages
- [ ] Create installer
- [ ] Test on multiple Windows versions
- [ ] Security audit
- [ ] Documentation review

## Recommended Order of Implementation

1. **Convert PNG to ICO** (quick win)
2. **Implement OPC DA Client** (critical path)
3. **Implement OPC UA Server** (critical path)
4. **Connect bridge logic** (critical path)
5. **Test end-to-end** (validation)
6. **Implement tag browser** (nice-to-have)
7. **Create MSI installer** (deployment)
8. **Polish and documentation** (finalize)

## Resources Needed

### Development
- OPC DA test server (Matrikon OPC Simulation Server - free)
- OPC UA test client (UA Expert - free)
- Windows VM for clean testing

### Libraries
- Decision on OPC DA library (free vs commercial)
- Budget: ~$0-1000 depending on library choice

### Time Estimate
- With commercial libraries: 1-2 weeks
- With free libraries: 2-3 weeks (more integration work)

## Questions to Resolve

1. **OPC DA Library**: Which library to use?
   - Technosoftware (commercial, easier)
   - OPC Foundation (free, more complex)
   - Other options?

2. **Security**: Which security profiles to support?
   - None (easiest, local networks)
   - Sign + Encrypt (most secure, complex)

3. **Deployment**: Priority on installer vs portable?
   - Both (recommended)
   - MSI only
   - Portable only

4. **Support**: What level of customer support planned?
   - Email only
   - Phone support
   - Remote assistance

---

**Contact for questions: dev@dhcautomation.com**
