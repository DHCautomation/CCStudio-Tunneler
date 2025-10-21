# CCStudio-Tunneler User Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Quick Start](#quick-start)
4. [Configuration](#configuration)
5. [Using the System Tray Application](#using-the-system-tray-application)
6. [Troubleshooting](#troubleshooting)
7. [FAQ](#faq)

## Introduction

CCStudio-Tunneler is a Windows application that bridges legacy OPC DA (DCOM-based) servers to modern OPC UA infrastructure. This enables:

- Cross-platform access to OPC DA data
- Integration with cloud-based systems like Mango M2M2
- Modernizing Building Automation Systems
- Bidirectional data flow (read and write)

### System Requirements

- **Operating System**: Windows 10/11 or Windows Server 2016+
- **RAM**: 512 MB minimum (1 GB recommended)
- **Disk Space**: 100 MB
- **Network**: TCP port 4840 (configurable)
- **Prerequisites**:
  - .NET 6.0 Runtime or later
  - OPC Core Components 3.0 (for OPC DA)

## Installation

### MSI Installer (Recommended)

1. Download `CCStudio-Tunneler-Setup.msi` from the releases page
2. Right-click the installer and select "Run as Administrator"
3. Follow the installation wizard:
   - Accept the license agreement
   - Choose installation directory (default: `C:\Program Files\DHC Automation\CCStudio-Tunneler`)
   - Select components (Service + Tray App recommended)
   - Allow firewall rule creation
4. Click "Install"
5. The service will start automatically after installation

### Portable Version

1. Download `CCStudio-Tunneler-Portable.zip`
2. Extract to your desired location
3. Run `CCStudio.Tunneler.TrayApp.exe` as Administrator
4. The service can be installed manually using:
   ```powershell
   sc create CCStudioTunneler binPath= "C:\Path\To\CCStudio.Tunneler.Service.exe"
   ```

## Quick Start

### Step 1: Launch the Configuration UI

1. Look for the CCStudio-Tunneler icon in your system tray (green plug icon)
2. Right-click the icon
3. Select **"Configure..."**

### Step 2: Configure OPC DA Source

In the **OPC DA Source** tab:

1. **Server ProgID**: Select or enter your OPC DA server's ProgID
   - Example: `Matrikon.OPC.Simulation.1`
   - You can browse available servers using the dropdown
2. **Server Host**: Enter the hostname or IP address
   - Use `localhost` for local servers
   - Use IP address or hostname for remote servers
3. **Update Rate**: Set the polling interval in milliseconds
   - Default: 1000ms (1 second)
   - Minimum: 100ms
4. **Dead Band**: Set the percentage change threshold (0-100)
   - Default: 0 (send all changes)
   - Higher values reduce network traffic
5. Click **"Test Connection"** to verify connectivity

### Step 3: Configure OPC UA Server

In the **OPC UA Server** tab:

1. **Server Port**: Set the TCP port (default: 4840)
2. **Server Name**: Display name shown to clients
3. **Endpoint URL**: Full OPC UA endpoint (auto-generated)
4. **Security Mode**: Choose security level
   - **None**: No encryption (fastest, local networks only)
   - **Sign**: Message signing
   - **Sign and Encrypt**: Full encryption (most secure)
5. **Allow Anonymous**: Check for no authentication
   - Uncheck and provide username/password for secured access

### Step 4: Map Tags (Optional)

In the **Tag Mapping** tab:

1. Click **"Browse DA Tags"** to discover available tags
2. Select tags you want to expose via OPC UA
3. Optionally rename tags for the OPC UA namespace
4. Set access level (Read, Write, or ReadWrite)
5. Add engineering units if needed

### Step 5: Apply Configuration

1. Click **"Apply"** to save changes
2. Restart the service from the system tray:
   - Right-click → Service → Restart Service

### Step 6: Connect Your OPC UA Client

Connect your OPC UA client (e.g., Mango M2M2) to:

```
opc.tcp://[your-server-ip]:4840
```

Example:
- Local: `opc.tcp://localhost:4840`
- Network: `opc.tcp://192.168.1.100:4840`

## Configuration

### Configuration File Location

The configuration is stored in JSON format at:

```
C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json
```

### Sample Configuration

```json
{
  "OpcDa": {
    "ServerProgId": "Matrikon.OPC.Simulation.1",
    "ServerHost": "localhost",
    "UpdateRate": 1000,
    "DeadBand": 0.0,
    "Tags": ["*"],
    "AutoDiscoverTags": true
  },
  "OpcUa": {
    "ServerPort": 4840,
    "ServerName": "CCStudio Tunneler",
    "EndpointUrl": "opc.tcp://localhost:4840",
    "SecurityMode": "None",
    "AllowAnonymous": true
  },
  "Logging": {
    "Level": "Information",
    "Path": "C:\\ProgramData\\DHC Automation\\CCStudio-Tunneler\\Logs",
    "RetentionDays": 7
  }
}
```

### Advanced Tag Mapping

Tag mappings support advanced features:

- **Aliasing**: Rename tags for OPC UA
- **Scaling**: Apply linear scaling (y = mx + b)
- **Type Conversion**: Force specific data types
- **Access Control**: Read-only, write-only, or read-write

Example tag mapping:

```json
{
  "DaTagName": "Channel1.Device1.Temperature",
  "UaNodeName": "Building.HVAC.Zone1.Temp",
  "AccessLevel": "ReadWrite",
  "ScaleFactor": 1.8,
  "Offset": 32,
  "EngineeringUnits": "°F",
  "Description": "Zone 1 Temperature in Fahrenheit"
}
```

## Using the System Tray Application

### System Tray Icon

The icon color indicates status:
- **Green**: Service running, all connections healthy
- **Yellow**: Service starting or reconnecting
- **Red**: Service stopped or error
- **Gray**: Service not installed

### Context Menu Options

- **Configure**: Open configuration window
- **View Status**: Show real-time service statistics
- **Service**:
  - Start Service
  - Stop Service
  - Restart Service
- **View Logs**: Open log directory
- **Open Config Folder**: Open configuration directory
- **About**: Show version and product information
- **Exit**: Close tray application (service continues running)

### Viewing Service Status

1. Right-click system tray icon
2. Select **"View Status"**

The status window shows:
- Service state and uptime
- OPC DA connection status
- OPC UA server status
- Active tag count
- Connected clients
- Message throughput
- Error count and last error

### Viewing Logs

Logs are stored at:
```
C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs
```

To view logs:
1. Right-click system tray icon
2. Select **"View Logs"**

Log files are rotated daily and retained for 7 days (configurable).

## Troubleshooting

### Service Won't Start

**Problem**: Service fails to start or immediately stops

**Solutions**:
1. Verify OPC Core Components are installed:
   - Download from OPC Foundation website
   - Install OPC Core Components 3.0
2. Check Windows Event Viewer:
   - Open Event Viewer (eventvwr.msc)
   - Navigate to Windows Logs → Application
   - Look for CCStudio-Tunneler errors
3. Verify OPC DA server is running:
   - Check server ProgID is correct
   - Ensure OPC DA server service is started
4. Run as Administrator:
   - Right-click tray app and "Run as Administrator"

### Can't Connect from OPC UA Client

**Problem**: OPC UA client cannot connect to CCStudio-Tunneler

**Solutions**:
1. Check firewall rules:
   ```powershell
   New-NetFirewallRule -DisplayName "CCStudio-Tunneler" -Direction Inbound -Protocol TCP -LocalPort 4840 -Action Allow
   ```
2. Verify endpoint URL matches:
   - Client endpoint: `opc.tcp://[server-ip]:4840`
   - Server configuration: Check "Endpoint URL" setting
3. Test connectivity:
   ```powershell
   Test-NetConnection -ComputerName [server-ip] -Port 4840
   ```
4. Check security settings:
   - Ensure client security mode matches server
   - Try "None" security mode for testing

### Tags Not Updating

**Problem**: OPC UA client shows stale or no data

**Solutions**:
1. Verify OPC DA connection:
   - Check "View Status" window
   - Ensure DA connection status is "Connected"
2. Check tag mappings:
   - Open Configuration → Tag Mapping
   - Verify tags are enabled
   - Ensure tag names match OPC DA server
3. Review update rate:
   - Configuration → OPC DA Source → Update Rate
   - Lower value = faster updates (minimum 100ms)
4. Check dead band:
   - Set to 0 to disable dead band filtering
   - Higher values require larger changes before update

### High CPU or Memory Usage

**Problem**: Service consumes excessive resources

**Solutions**:
1. Reduce tag count:
   - Map only required tags
   - Use tag filters to exclude unused tags
2. Increase update rate:
   - Higher value = less frequent polling
   - Balance between performance and responsiveness
3. Enable dead band:
   - Set 1-5% to filter minor changes
   - Reduces unnecessary updates
4. Review log level:
   - Change from "Debug" to "Information" or "Warning"

### Permission Errors

**Problem**: Access denied or permission errors

**Solutions**:
1. Run as Administrator:
   - Required for service control
   - Required for DCOM/OPC DA access
2. Configure DCOM permissions:
   ```
   dcomcnfg.exe → Component Services → Computers → My Computer → Properties → COM Security
   ```
3. Add user to OPC DA server permissions:
   - Check OPC DA server security settings
   - Add service account to allowed users

## FAQ

### Q: Can I connect multiple OPC UA clients?

**A**: Yes! CCStudio-Tunneler supports up to 100 simultaneous client connections (configurable).

### Q: Does this work with Mango M2M2?

**A**: Yes! Mango's OPC UA driver can connect directly to CCStudio-Tunneler. Use the OPC UA endpoint URL in Mango's data source configuration.

### Q: Can I run multiple instances on the same machine?

**A**: Yes, but each instance must use a different port. Change the "Server Port" in OPC UA configuration.

### Q: Is bidirectional (read/write) supported?

**A**: Yes! OPC UA clients can read from and write to OPC DA tags if configured with "ReadWrite" access.

### Q: Can I connect to remote OPC DA servers?

**A**: Yes! Set "Server Host" to the remote server's IP or hostname. Note: DCOM must be properly configured for remote access.

### Q: How do I backup my configuration?

**A**:
1. Export configuration from the Configuration window
2. Or copy `C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json`

### Q: Does this work on Linux?

**A**: No. OPC DA requires Windows and DCOM. However, CCStudio-Tunneler enables Linux machines to **access** OPC DA data via OPC UA.

### Q: What happens if the OPC DA server goes offline?

**A**: CCStudio-Tunneler will automatically attempt to reconnect with exponential backoff. OPC UA clients will see "Bad" quality data during disconnection.

### Q: Can I monitor the service remotely?

**A**: Currently, monitoring is local via the tray application. Remote monitoring via web interface is planned for future releases.

### Q: Is there a command-line interface?

**A**: Service control is available via Windows Service Manager (`services.msc`) or PowerShell:
```powershell
Start-Service CCStudioTunneler
Stop-Service CCStudioTunneler
Restart-Service CCStudioTunneler
```

---

**For additional support, contact: support@dhcautomation.com**
