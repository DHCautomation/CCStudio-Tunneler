# CCStudio-Tunneler v1.1 - Quick Reference Card

## 🚀 Quick Start (5 Minutes)

```powershell
# 1. Copy example config
copy examples\matrikon-simulation-test.json "C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json"

# 2. Run service
cd src\CCStudio.Tunneler.Service
dotnet run

# 3. Connect UA Expert to opc.tcp://localhost:4840
# 4. Browse Objects → CCStudioTunneler → Simulation
```

---

## 📁 File Locations

| File | Location |
|------|----------|
| Configuration | `C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json` |
| Logs | `C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs\log-*.txt` |
| Templates | `templates/ASI_Controls.json`, etc. |
| Examples | `examples/single-server-asi-controls.json`, etc. |

---

## ⚙️ Configuration Snippets

### Single Server (Legacy/Backward Compatible)
```json
{
  "OpcDa": {
    "ServerProgId": "ASI.OPCServer.1",
    "ServerHost": "localhost",
    "UpdateRate": 10000
  },
  "OpcUa": {
    "EndpointUrl": "opc.tcp://localhost:4840"
  },
  "TagMappings": [
    {
      "DaTagName": "Building.AHU_01.Temp",
      "UaNodeName": "HVAC/AHU01/Temp",
      "Enabled": true,
      "AccessLevel": "Read",
      "UpdateRate": 30000,
      "Deadband": 0.5
    }
  ]
}
```

### Multi-Server (v1.1 NEW!)
```json
{
  "OpcDaSources": [
    {
      "Id": "HVAC",
      "Name": "Building HVAC",
      "UaNamespace": "HVAC",
      "Enabled": true,
      "Configuration": {
        "ServerProgId": "ASI.OPCServer.1",
        "ServerHost": "localhost"
      }
    },
    {
      "Id": "Lighting",
      "Name": "Building Lighting",
      "UaNamespace": "Lighting",
      "Enabled": true,
      "Configuration": {
        "ServerProgId": "Lighting.OPCServer.1",
        "ServerHost": "10.1.1.51"
      }
    }
  ]
}
```

---

## 🔧 Common Tasks

### Change Server Connection
```json
"ServerProgId": "YOUR.PROGID.HERE",
"ServerHost": "10.1.1.50"
```

### Change OPC UA Endpoint
```json
"EndpointUrl": "opc.tcp://192.168.1.100:4840"
```

### Add Tag Mapping
```json
{
  "DaTagName": "Building.AHU.Temp",
  "UaNodeName": "HVAC/AHU/Temp",
  "Enabled": true,
  "AccessLevel": "ReadWrite",
  "UpdateRate": 30000,
  "Deadband": 0.5,
  "EngineeringUnits": "degF",
  "MinValue": 40.0,
  "MaxValue": 85.0,
  "ServerId": "HVAC"  // For multi-server
}
```

### Optimize Performance
```json
// Critical alarm - fast
{ "UpdateRate": 1000, "Deadband": 0.0 }

// Status - medium
{ "UpdateRate": 5000, "Deadband": 0.0 }

// Temperature - slow
{ "UpdateRate": 30000, "Deadband": 0.5 }

// Outdoor temp - very slow
{ "UpdateRate": 60000, "Deadband": 1.0 }
```

---

## 🔍 Troubleshooting Commands

### Check if OPC DA ProgID exists
```powershell
# Registry Editor
regedit → HKEY_CLASSES_ROOT → Search for ProgID
```

### Check if port 4840 is in use
```powershell
netstat -ano | findstr :4840
```

### Create firewall rule
```powershell
New-NetFirewallRule -DisplayName "CCStudio-Tunneler" -Direction Inbound -Protocol TCP -LocalPort 4840 -Action Allow
```

### View real-time logs
```powershell
Get-Content "C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs\log-*.txt" -Wait -Tail 50
```

---

## 📊 Expected Log Messages

### ✅ Success
```
✓ Configuration validated successfully
✓ OPC UA Server started successfully
✓ Connected to OPC DA Server: [Name]
✓ Subscribed to X tags from [Server]
✓ CCStudio-Tunneler is now running
  - OPC UA Server: opc.tcp://localhost:4840
  - OPC DA Servers: X/X connected
```

### ⚠️ Warnings (Normal During Reconnect)
```
✗ Failed to connect to OPC DA Server - will retry later
Attempting to reconnect to OPC DA Server: [Name]
Reconnection attempt 1/∞, next delay: 5s
```

### ❌ Errors (Need Attention)
```
✗ OPC Server ProgID not found: [ProgID]
✗ Failed to start OPC UA Server
COM Error: 0x800706BA - RPC server unavailable
```

---

## 🏷️ Access Levels

| Value | Meaning | Use For |
|-------|---------|---------|
| `Read` | Read-only | Sensors, status |
| `Write` | Write-only | Commands (rare) |
| `ReadWrite` | Bi-directional | Setpoints, controls |

---

## 📈 Update Rate Guidelines

| Tag Type | Rate (ms) | Deadband (%) |
|----------|-----------|--------------|
| Critical alarms | 1000 | 0 |
| Equipment status | 5000 | 0 |
| Fan speeds, dampers | 10000 | 2-5 |
| Temperatures, pressures | 30000 | 0.5-1.0 |
| Outdoor conditions | 60000 | 1.0 |

---

## 🔒 Security Quick Config

### No Security (Testing/Local Network)
```json
"SecurityMode": "None",
"AllowAnonymous": true
```

### Basic Security (Production)
```json
"SecurityMode": "SignAndEncrypt",
"AllowAnonymous": false
```

---

## 🐛 Common Issues & Fixes

| Problem | Solution |
|---------|----------|
| Can't connect to DA server | Check ProgID in registry, verify server running, fix DCOM |
| OPC UA port in use | Change port or kill process using it |
| Tags not updating | Check update rate, deadband, tag enabled status |
| High CPU usage | Increase update rates, reduce tag count, increase deadbands |
| Service won't start | Run as Administrator, check .NET installed, review logs |

---

## 📞 Getting Help

1. **Check Logs**: `C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs\`
2. **Review Documentation**:
   - TESTING_GUIDE.md (comprehensive testing)
   - IMPLEMENTATION_SUMMARY.md (feature details)
   - FOCUSED_IMPROVEMENTS.md (roadmap)
   - examples/README.md (configuration examples)

3. **Report Issues**:
   - GitHub: https://github.com/DHCautomation/CCStudio-Tunneler/issues
   - Email: support@dhcautomation.com

---

## 🎯 Success Indicators

✅ **Service Running**:
- Logs show: `✓ CCStudio-Tunneler is now running`
- OPC UA server listening on port 4840
- All DA servers connected

✅ **UA Expert Working**:
- Can connect to endpoint
- Browse CCStudioTunneler folder
- See tags with namespaces
- Values updating
- Can write to ReadWrite tags

✅ **Mango M2M2 Working**:
- Data source connects
- Tags discovered
- Data points update
- Alarms trigger on quality changes
- Writes reach OPC DA server

---

## 📦 What's New in v1.1

- ✨ **Multi-Server Support** - Connect to multiple OPC DA servers simultaneously
- ✨ **Namespace Separation** - Organize tags (HVAC/, Lighting/, etc.)
- ✨ **Per-Tag Update Rates** - Optimize bandwidth per tag type
- ✨ **Enhanced Quality Mapping** - Detailed OPC DA → UA quality codes
- ✨ **Auto-Reconnect** - Exponential backoff (5s → 10s → 20s → ...)
- ✨ **Config Validation** - Startup validation with clear error messages
- ✨ **Better Logging** - Structured logs with checkmarks and categories
- ✨ **Example Configs** - Ready-to-use templates for ASI, JCI, Honeywell, Siemens

---

## 💡 Pro Tips

1. **Start with Matrikon** - Test with simulator before real hardware
2. **Use Templates** - Copy from `templates/` directory
3. **Enable Debug Logging** - For troubleshooting: `"Level": "Debug"`
4. **Monitor Heartbeat** - Every 60s shows uptime, messages, connections
5. **Backup Config** - Before making changes
6. **Test Writes Carefully** - Verify setpoints before deploying to production
7. **Use Namespaces** - Organize tags logically for Mango
8. **Optimize Rates** - Don't poll everything at 1 second
9. **Check Ranges** - Use MinValue/MaxValue to catch errors
10. **Read Logs** - They tell you everything

---

**Version**: 1.1.0
**Date**: 2025-10-21
**License**: MIT
**Developer**: DHC Automation and Controls

---

**Ready to test? See TESTING_GUIDE.md for step-by-step instructions!**
