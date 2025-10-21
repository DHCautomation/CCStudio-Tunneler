# Configuration Templates for CCStudio-Tunneler

This directory contains pre-configured templates for common building automation systems. These templates provide a quick start for integrating with popular BAS platforms.

## Available Templates

### 1. ASI_Controls.json
**System**: ASI Controls (WebCTRL-based)
**Common In**: Schools, hospitals, commercial buildings
**ProgID**: `ASI.OPCServer.1`

**Includes**:
- AHU supply/return temperatures
- Fan speeds and status
- VAV box damper positions and zone temperatures
- Chiller and boiler temperatures
- Optimized update rates (30-60 seconds for temps, 5 seconds for status)
- Deadband settings to reduce network traffic

### 2. Johnson_Controls_Metasys.json
**System**: Johnson Controls Metasys
**Common In**: Large commercial buildings, campuses
**ProgID**: `JohnsonControls.Metasys.OPCServer`

**Includes**:
- Site hierarchy patterns (`Site.*.*`)
- Network Automation Engine (NAE) patterns (`NAE*.*.*`)
- Auto-discovery enabled

### 3. Honeywell_EBI.json
**System**: Honeywell Enterprise Buildings Integrator
**Common In**: Enterprise facilities
**ProgID**: `Honeywell.EBI.OPCServer`

**Includes**:
- Generic point patterns for customization
- 10-second update rate
- Auto-discovery enabled

### 4. Siemens_Desigo.json
**System**: Siemens Desigo
**Common In**: International buildings, large facilities
**ProgID**: `Siemens.Desigo.OPC`

**Includes**:
- Generic point patterns for customization
- 5-second update rate
- Auto-discovery enabled

### 5. Multi_Server_Example.json
**Example**: Multiple OPC DA servers connected simultaneously
**Purpose**: Demonstration of multi-server capability

**Shows**:
- HVAC system (localhost)
- Lighting system (remote server at 10.1.1.51)
- Security system (remote server at 10.1.1.52)
- Namespace separation (HVAC/, Lighting/, Security/)
- Different update rates per system

## How to Use Templates

### Method 1: Copy to Configuration Directory

1. Choose the appropriate template for your system
2. Copy it to the configuration directory:
   ```
   copy templates\ASI_Controls.json "C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json"
   ```
3. Edit `config.json` to customize:
   - Server ProgID (if different)
   - Server host/IP address
   - Endpoint URL
   - Tag patterns

### Method 2: Load from UI (Future Feature)

The configuration wizard will have a "Load Template" dropdown to select and customize templates.

## Customizing Templates

After loading a template, customize these settings:

### 1. Server Connection
```json
"ServerProgId": "YOUR.OPC.SERVER.PROGID",
"ServerHost": "localhost",  // or IP address
```

### 2. OPC UA Endpoint
```json
"EndpointUrl": "opc.tcp://YOUR-IP:4840",
```

### 3. Tag Patterns
Replace wildcard patterns with actual tag names after browsing:
```json
"DaTagName": "Building_01.AHU_01.SupplyAirTemp",  // Specific tag
```

Or use patterns for bulk mapping:
```json
"DaTagName": "Building_01.AHU_*.SupplyAirTemp",  // All AHUs in Building 1
```

### 4. Update Rates
Adjust per your needs:
- **Critical alarms**: 1000-2000 ms
- **Equipment status**: 5000 ms
- **Analog values** (temps, pressures): 30000-60000 ms
- **Slowly changing** (outdoor temp): 60000-300000 ms

### 5. Deadband
Set to reduce unnecessary updates:
- **Temperature**: 0.5-1.0 °F
- **Percentage** (fan speed, damper): 2-5 %
- **Digital** (on/off): 0.0 (always update on change)

## Testing Templates

After loading a template:

1. **Verify ProgID**: Check if OPC DA server is registered
   ```
   regedit → HKEY_CLASSES_ROOT → search for ProgID
   ```

2. **Test Connection**: Start service and check logs
   ```
   C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs\
   ```

3. **Browse Tags**: Use tag browser to discover actual tag names

4. **Connect Mango**:
   ```
   opc.tcp://YOUR-SERVER-IP:4840
   ```

5. **Monitor in Mango**: Verify tag values are updating

## Common ProgIDs

If your ProgID is different, check the registry or ask your integrator:

| Vendor | Common ProgIDs |
|--------|----------------|
| ASI Controls | `ASI.OPCServer.1`, `ASI.OPCDA.Server` |
| Johnson Controls | `JohnsonControls.Metasys.OPCServer`, `JCI.Metasys.OPC` |
| Honeywell | `Honeywell.EBI.OPCServer`, `Honeywell.OPCDA` |
| Siemens | `Siemens.Desigo.OPC`, `OPC.SimaticNet` |
| Schneider Electric | `Schneider.OPCFactory.Server`, `StruxureWare.OPC` |
| Tridium JACE | `Tridium.OPCServer`, `JACE.OPC` |
| Automated Logic | `ALC.WebCTRL.OPC`, `WebCTRL.OPCServer` |

## Tag Naming Conventions

Different systems use different hierarchies:

### ASI Controls / WebCTRL
```
Building_01.AHU_01.SupplyAirTemp
Building_01.Floor_02.VAV_201.SpaceTemp
```

### Johnson Controls Metasys
```
Site.Building1.AHU1.SupplyTemp
NAE01.AHU1.SupplyAirTemp
```

### Honeywell EBI
```
Building1:AHU1:SupplyTemp
Building1.HVAC.AHU_01.SAT
```

### Siemens Desigo
```
Plant.Building1.AHU_01.T_SA
HVACSystem.AHU1.SupplyAirTemperature
```

## Best Practices

1. **Start with Auto-Discovery**: Let the system discover tags, then refine
2. **Use Wildcards Initially**: `AHU_*` to get all AHUs, then customize
3. **Test with Few Tags First**: Subscribe to 10-20 tags, verify working, then scale up
4. **Optimize Update Rates**: Don't poll everything at 1 second
5. **Use Deadbands**: Reduce network traffic by 50-90%
6. **Group by System**: Use multi-server for HVAC + Lighting + Security
7. **Document Mappings**: Add descriptions to tag mappings

## Troubleshooting

### Template Won't Load
- Check JSON syntax (use JSON validator)
- Ensure all required fields are present
- Check file permissions

### Can't Connect to OPC DA Server
- Verify ProgID in registry
- Check DCOM permissions
- Ensure OPC Core Components installed
- Try from OPC Test Client first

### No Tags Discovered
- Check tag patterns (wildcards)
- Verify auto-discovery enabled
- Try browsing manually first
- Check include/exclude filters

## Creating Custom Templates

To create your own template:

1. Start with `Multi_Server_Example.json`
2. Customize for your system
3. Test thoroughly
4. Save in `templates/` directory
5. Add `.gitignore` entry if contains sensitive data

Example:
```json
{
  "Name": "My Custom System",
  "OpcDaSources": [
    {
      "Id": "MySystem",
      "Configuration": {
        "ServerProgId": "My.OPC.Server.1",
        // ... customize
      }
    }
  ]
}
```

## Support

If you need help with templates:
1. Check system documentation for ProgID
2. Use OPC Test Client to verify connection
3. Contact DHC Automation for custom template creation
4. Post in community forum

---

**Last Updated**: 2025-10-21
**Version**: 1.0
