# Example Configurations

This directory contains working example configurations for CCStudio-Tunneler.

## Available Examples

### 1. single-server-asi-controls.json
**Purpose**: Single ASI Controls server (backward compatible mode)
**Use When**: You have one OPC DA server to connect to
**Features**:
- Legacy `OpcDa` configuration format
- Backward compatible with v1.0
- Example tag mappings with optimized update rates
- Per-tag deadband and validation ranges

**Quick Start**:
```powershell
copy examples\single-server-asi-controls.json "C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json"
```

Edit the file and change:
- `ServerProgId`: Your OPC DA server ProgID
- `ServerHost`: `localhost` or IP address
- `EndpointUrl`: Your server's IP address
- `TagMappings`: Your actual tag names

---

### 2. multi-server-example.json
**Purpose**: Multiple OPC DA servers with namespace separation
**Use When**: You need to connect to multiple systems (HVAC + Lighting, etc.)
**Features**:
- Uses new `OpcDaSources` array
- Namespace separation (`HVAC/`, `Lighting/`)
- Different servers can have different update rates
- Tags mapped to specific servers via `ServerId`

**Quick Start**:
```powershell
copy examples\multi-server-example.json "C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json"
```

Edit each server in `OpcDaSources`:
- Change `ServerProgId`, `ServerHost` for each system
- Customize `UaNamespace` for organization
- Update tag mappings with correct `ServerId`

**Mango Will See**:
```
opc.tcp://your-server:4840
├─ HVAC/
│  ├─ AHU01/SupplyTemp
│  └─ AHU01/FanSpeed
└─ Lighting/
   ├─ Floor1/Zone1/LightLevel
   └─ Floor1/Zone1/Occupancy
```

---

### 3. matrikon-simulation-test.json
**Purpose**: Testing with Matrikon OPC Simulation Server
**Use When**: You want to test CCStudio-Tunneler without real hardware
**Features**:
- Pre-configured for Matrikon simulator
- Includes readable and writable test tags
- Debug logging enabled
- 1-second update rates for testing

**Prerequisites**:
1. Download and install Matrikon OPC Simulation Server (free):
   https://www.matrikonopc.com/downloads/176/productsoftware/index.aspx

2. Start Matrikon simulator

3. Use this config:
   ```powershell
   copy examples\matrikon-simulation-test.json "C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json"
   ```

4. Start CCStudio-Tunneler service

5. Connect UA Expert to `opc.tcp://localhost:4840`

6. Verify tags updating: `Simulation/RandomInt1`, `Simulation/RandomReal4`, etc.

7. Test write: Write to `Simulation/BucketInt1` from UA Expert

---

## Configuration Anatomy

### Basic Structure

```json
{
  // Single server mode (legacy)
  "OpcDa": {
    "ServerProgId": "Your.OPC.Server.ProgID",
    "ServerHost": "localhost",
    "UpdateRate": 10000,
    "DeadBand": 1.0,
    "Tags": ["*"],
    "ConnectionTimeout": 30,
    "MaxReconnectAttempts": 0,
    "ReconnectDelay": 5
  },

  // OR Multi-server mode (v1.1+)
  "OpcDaSources": [
    {
      "Id": "unique_id",
      "Name": "Friendly Name",
      "UaNamespace": "NamespacePrefix",
      "Enabled": true,
      "Configuration": {
        // Same as OpcDa above
      }
    }
  ],

  "OpcUa": {
    "ServerPort": 4840,
    "EndpointUrl": "opc.tcp://YOUR-IP:4840",
    "SecurityMode": "None",
    "AllowAnonymous": true
  },

  "Logging": {
    "Level": "Information",  // Debug, Information, Warning, Error
    "Path": "C:\\ProgramData\\DHC Automation\\CCStudio-Tunneler\\Logs",
    "RetentionDays": 7
  },

  "TagMappings": [
    {
      "DaTagName": "OPC.DA.Tag.Name",
      "UaNodeName": "OPC/UA/Node/Path",
      "Enabled": true,
      "AccessLevel": "ReadWrite",  // Read, Write, ReadWrite
      "UpdateRate": 10000,         // Override global rate (ms)
      "Deadband": 1.0,             // Only update if changed by %
      "ServerId": "unique_id",     // For multi-server
      "EngineeringUnits": "degF",
      "MinValue": 0.0,
      "MaxValue": 100.0
    }
  ]
}
```

---

## Common Customizations

### 1. Change Server Connection

```json
"OpcDa": {
  "ServerProgId": "YOUR_PROGID_HERE",
  "ServerHost": "10.1.1.50",  // Or "localhost"
  "UpdateRate": 10000
}
```

### 2. Change OPC UA Endpoint

```json
"OpcUa": {
  "ServerPort": 4840,
  "EndpointUrl": "opc.tcp://192.168.1.100:4840"  // Your server's IP
}
```

### 3. Enable Security

```json
"OpcUa": {
  "SecurityMode": "SignAndEncrypt",  // Instead of "None"
  "AllowAnonymous": false,
  "Username": "mango",
  "Password": "your-password"
}
```

### 4. Add Tag Mappings

```json
"TagMappings": [
  {
    "DaTagName": "Building_01.AHU_01.SupplyAirTemp",
    "UaNodeName": "HVAC/AHU01/SupplyTemp",
    "Enabled": true,
    "AccessLevel": "Read",
    "UpdateRate": 30000,    // 30 seconds
    "Deadband": 0.5,        // 0.5% change required
    "EngineeringUnits": "degF",
    "MinValue": 40.0,
    "MaxValue": 85.0
  }
]
```

### 5. Optimize Update Rates

| Tag Type | Recommended Rate | Deadband |
|----------|------------------|----------|
| Critical alarms | 1000 ms | 0% |
| Equipment status | 5000 ms | 0% |
| Analog sensors (temps) | 30000 ms | 0.5-1.0% |
| Slow-changing (outdoor temp) | 60000 ms | 1.0% |

Example:
```json
{
  "DaTagName": "HighTempAlarm",
  "UpdateRate": 1000,
  "Deadband": 0.0
},
{
  "DaTagName": "OutdoorTemp",
  "UpdateRate": 60000,
  "Deadband": 1.0
}
```

---

## Testing Your Configuration

### 1. Validate JSON Syntax
Use an online validator: https://jsonlint.com/

### 2. Check Logs
After starting the service:
```
C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs\log-YYYYMMDD.txt
```

Look for:
```
✓ Configuration validated successfully
✓ OPC UA Server started successfully
✓ Connected to OPC DA Server: Your Server Name
✓ Subscribed to 10 tags from Your Server Name
```

### 3. Test with UA Expert

1. Download UA Expert (free): https://www.unified-automation.com/products/development-tools/uaexpert.html

2. Connect to your server:
   - Add Server: `opc.tcp://localhost:4840`
   - Connect (accept certificate if prompted)

3. Browse address space:
   - Expand "Objects"
   - Look for "CCStudioTunneler" folder
   - Your namespaces should appear (HVAC/, Lighting/, etc.)

4. Subscribe to tags:
   - Drag tags to Data Access View
   - Verify values updating

5. Test write:
   - Right-click writable tag
   - Write Value
   - Check if OPC DA server received it

### 4. Connect Mango M2M2

1. In Mango, add OPC UA Data Source:
   - Endpoint URL: `opc.tcp://YOUR-SERVER-IP:4840`
   - Security: None (or configure)
   - Connect

2. Discover tags:
   - Browse address space
   - See your tags organized by namespace

3. Create data points:
   - Add points to Mango
   - Verify values updating

4. Test setpoints:
   - Write to a ReadWrite point from Mango
   - Verify write reaches OPC DA server

---

## Troubleshooting

### "OPC UA Server failed to start"
- Check if port 4840 is already in use
- Check firewall rules
- Review logs for certificate errors

### "Failed to connect to OPC DA Server"
- Verify ProgID is correct (check registry: `HKEY_CLASSES_ROOT`)
- Ensure OPC DA server is running
- Check DCOM permissions
- Verify OPC Core Components installed

### "No tags discovered"
- Change `AutoDiscoverTags` to `false`
- Manually add tags to `TagMappings`
- Check tag names match exactly (case-sensitive)

### "Tags not updating"
- Check `UpdateRate` settings
- Verify `Deadband` isn't too large
- Check tag quality in logs (Good/Bad/Uncertain)
- Ensure tags are `Enabled: true`

### "Can't write to tags"
- Verify `AccessLevel: "ReadWrite"` or `"Write"`
- Check OPC DA server allows writes
- Check DCOM permissions for writing

---

## Migration from v1.0 to v1.1

If you have an existing config.json from v1.0:

**It will still work!** Backward compatible.

But to use multi-server features:

1. **Backup your config**:
   ```powershell
   copy config.json config-backup.json
   ```

2. **Convert to multi-server** (optional):
   ```json
   // OLD (v1.0)
   "OpcDa": {
     "ServerProgId": "ASI.OPCServer.1",
     ...
   }

   // NEW (v1.1) - converts automatically, or manually:
   "OpcDaSources": [
     {
       "Id": "default",
       "Name": "ASI Controls",
       "UaNamespace": "",  // No prefix in legacy mode
       "Enabled": true,
       "Configuration": {
         "ServerProgId": "ASI.OPCServer.1",
         ...
       }
     }
   ]
   ```

3. **Add second server** (if needed):
   ```json
   "OpcDaSources": [
     { /* first server */ },
     {
       "Id": "lighting",
       "Name": "Lighting System",
       "UaNamespace": "Lighting",
       "Enabled": true,
       "Configuration": {
         "ServerProgId": "Lighting.OPCServer.1",
         "ServerHost": "10.1.1.51",
         ...
       }
     }
   ]
   ```

---

## Best Practices

1. **Start Small**: Test with 5-10 tags before adding hundreds
2. **Use Namespaces**: Organize tags logically (Building/Floor/System/Point)
3. **Optimize Rates**: Don't poll everything at 1 second
4. **Use Deadbands**: Reduce unnecessary updates by 50-90%
5. **Document Tags**: Add descriptions and engineering units
6. **Validate Ranges**: Set MinValue/MaxValue to catch errors
7. **Monitor Logs**: Check regularly for errors or warnings
8. **Backup Config**: Save before making major changes

---

**Need Help?**
- Check IMPLEMENTATION_SUMMARY.md for detailed feature documentation
- Review logs in `C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs\`
- See FOCUSED_IMPROVEMENTS.md for roadmap and future features

**Last Updated**: 2025-10-21
