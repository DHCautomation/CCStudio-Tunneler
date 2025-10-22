# CCStudio-Tunneler: Focused Product Improvements
## OPC DA to OPC UA Bridge for Mango M2M2 Integration

**Date**: 2025-10-21
**Primary Use Case**: Enable Mango M2M2 to access legacy OPC DA systems (ASI Controls, etc.)

---

## Core Problem Being Solved

**The Challenge**:
- Legacy building automation systems (ASI Controls, older Johnson Controls, Honeywell, Siemens) use OPC DA
- OPC DA requires Windows DCOM (unreliable across networks, security nightmare)
- Mango M2M2 runs on Linux and uses OPC UA data sources
- Can't directly connect Mango to OPC DA servers

**The Solution**: CCStudio-Tunneler bridges OPC DA (local Windows) to OPC UA (network-friendly)

```
┌─────────────────┐         ┌──────────────────────┐         ┌─────────────────┐
│  ASI Controls   │◄─DCOM──►│  CCStudio-Tunneler   │◄─OPC UA─►│  Mango M2M2     │
│  OPC DA Server  │         │  (Windows Bridge)    │         │  (Linux/Java)   │
│  (Windows)      │         │                      │         │                 │
└─────────────────┘         └──────────────────────┘         └─────────────────┘
    Building Network              Same Windows PC              Building Network
```

---

## Priority Improvements for This Use Case

### 🔴 CRITICAL - Must Have for Production

#### 1. Robust OPC DA Tag Browser
**Priority**: CRITICAL | **Effort**: 3-5 days

**Problem**: Tech integrators need to discover what points exist in ASI/legacy systems. Current implementation has simplified browsing.

**Solution**: Full hierarchical OPC DA browser
```
ASI Controls Server
├─ Building_01
│   ├─ AHU_01
│   │   ├─ SupplyAirTemp       (Analog Input)
│   │   ├─ ReturnAirTemp       (Analog Input)
│   │   ├─ FanSpeed            (Analog Output)
│   │   └─ FanStatus           (Digital Input)
│   ├─ AHU_02
│   └─ VAV_Boxes
│       ├─ VAV_101_DamperPos
│       ├─ VAV_101_SpaceTemp
│       └─ ...
└─ Building_02
```

**Must Support**:
- ✅ Full hierarchical tree browsing (not just flat list)
- ✅ Search/filter by tag name
- ✅ Show data types (analog, digital, string)
- ✅ Live values while browsing (to verify correct point)
- ✅ Bulk selection ("Select all AHU_01 points")
- ✅ Export tag list to CSV (for documentation)

**Why Critical**: Integrators waste hours manually typing tag names. Need to browse like UA Expert does for OPC UA.

---

#### 2. Multi-Server Support
**Priority**: CRITICAL | **Effort**: 2-3 days

**Problem**: Large buildings have multiple OPC DA servers (HVAC controller, lighting system, security system).

**Current**: Only one server at a time.

**Solution**: Connect to multiple OPC DA servers, expose all via single OPC UA endpoint

```json
{
  "OpcDaSources": [
    {
      "Id": "HVAC_System",
      "ServerProgId": "ASI.OPCServer.1",
      "ServerHost": "localhost",
      "Tags": ["*"],
      "UaNamespace": "HVAC"
    },
    {
      "Id": "Lighting_System",
      "ServerProgId": "Lighting.OPCServer.1",
      "ServerHost": "10.1.1.51",
      "Tags": ["*"],
      "UaNamespace": "Lighting"
    }
  ],
  "OpcUa": {
    "ServerPort": 4840
  }
}
```

**Mango sees**:
```
opc.tcp://tunneler:4840
  ├─ HVAC/
  │   ├─ AHU_01/SupplyTemp
  │   └─ Chiller_01/Power
  └─ Lighting/
      ├─ Floor1_Zone1
      └─ Floor2_Zone1
```

**Benefits**:
- One Tunneler instance for entire building
- One OPC UA data source in Mango
- Easier to manage than multiple bridges

---

#### 3. Connection Health Monitoring & Auto-Reconnect
**Priority**: CRITICAL | **Effort**: 1-2 days

**Problem**: OPC DA servers crash, network blips happen. Tunneler must recover automatically.

**Enhanced Monitoring**:
```csharp
public class ConnectionHealth
{
    public string ServerId { get; set; }
    public ConnectionState State { get; set; } // Connected, Disconnected, Reconnecting, Failed
    public DateTime? LastConnected { get; set; }
    public DateTime? LastDisconnected { get; set; }
    public int ReconnectAttempts { get; set; }
    public TimeSpan Uptime { get; set; }
    public string LastError { get; set; }
}
```

**Reconnection Strategy**:
```json
{
  "OpcDa": {
    "AutoReconnect": true,
    "ReconnectStrategy": "ExponentialBackoff",
    "InitialRetryDelay": 5,
    "MaxRetryDelay": 300,
    "MaxReconnectAttempts": 0,  // 0 = infinite
    "HealthCheckInterval": 30
  }
}
```

**Backoff Schedule**:
- Attempt 1: 5 seconds
- Attempt 2: 10 seconds
- Attempt 3: 20 seconds
- Attempt 4: 40 seconds
- ...
- Max: 300 seconds (5 minutes)

**UI Indicator**: System tray icon changes color
- 🟢 Green: All servers connected
- 🟡 Yellow: Some servers reconnecting
- 🔴 Red: All servers disconnected
- ⚫ Gray: Service stopped

**Why Critical**: Techs can't babysit the bridge. Must recover from failures automatically.

---

#### 4. Tag Quality Mapping
**Priority**: CRITICAL | **Effort**: 1 day

**Problem**: OPC DA quality codes must map correctly to OPC UA so Mango knows if data is good.

**OPC DA Quality** → **OPC UA StatusCode**:
```csharp
// OPC DA Quality (0-255)
// Bits 7-6: Quality (00=Bad, 01=Uncertain, 11=Good)
// Bits 5-0: Substatus

var daQuality = 192; // 0xC0 = Good

// Map to OPC UA
StatusCode uaStatus = daQuality switch
{
    >= 192 => StatusCodes.Good,                    // Good
    >= 64 and < 192 => StatusCodes.Uncertain,      // Uncertain
    _ => StatusCodes.Bad                            // Bad
};
```

**Enhanced Mapping**:
- `Good` → `Good` (0x00000000)
- `Good (Local Override)` → `GoodLocalOverride`
- `Uncertain` → `Uncertain` (0x40000000)
- `Uncertain (Sensor Not Accurate)` → `UncertainSensorNotAccurate`
- `Bad` → `Bad` (0x80000000)
- `Bad (Not Connected)` → `BadNoCommunication`
- `Bad (Device Failure)` → `BadDeviceFailure`

**UI Display**: Show quality in tag browser and status window
```
Tag: AHU_01/SupplyTemp
Value: 72.5°F
Quality: ✓ Good
Timestamp: 2025-10-21 10:35:42
```

**Why Critical**: Mango needs to know if data is reliable. Bad quality data should trigger alarms in Mango.

---

#### 5. Data Type Handling
**Priority**: HIGH | **Effort**: 2 days

**Problem**: OPC DA has various data types. Must map correctly to OPC UA and Mango.

**Common Types in Building Automation**:

| OPC DA (VT_xxx) | OPC UA Type | Example |
|-----------------|-------------|---------|
| VT_R4 (float) | Float | 72.5 (temperature) |
| VT_R8 (double) | Double | 123.456789 |
| VT_I2 (short) | Int16 | 1-100 (percentage) |
| VT_I4 (int) | Int32 | 50000 (large numbers) |
| VT_BOOL | Boolean | ON/OFF, true/false |
| VT_BSTR (string) | String | "Running", "Alarm" |
| VT_DATE | DateTime | Timestamps |

**Edge Cases**:
- **Arrays**: Some controllers send array of values (e.g., 24-hour schedule)
- **Null/Empty**: Handle gracefully (don't crash)
- **Type Coercion**: If DA sends int but UA expects float, convert

**Configuration**:
```json
{
  "TagMappings": [
    {
      "DaTagName": "AHU_01.FanSpeed",
      "DaDataType": "VT_R4",
      "UaNodeName": "HVAC/AHU_01/FanSpeed",
      "UaDataType": "Float",
      "ForcedDataType": null,  // Optional: force type conversion
      "ValidRange": { "Min": 0, "Max": 100 }
    }
  ]
}
```

**Validation**: Warn if value out of range (e.g., temperature of 9999°F is likely error)

---

#### 6. Easy Installation for Field Techs
**Priority**: HIGH | **Effort**: 3-5 days

**Problem**: Field techs aren't .NET developers. Installation must be foolproof.

**MSI Installer Must**:
1. ✅ **Auto-detect and install .NET 8 Runtime** (if missing)
2. ✅ **Check for OPC Core Components** (prompt to download if missing)
3. ✅ **Configure DCOM automatically** (biggest pain point!)
   ```powershell
   # Set DCOM permissions for OPC
   dcomcnfg /RegServer
   # Allow local and remote activation
   # Add user to OPC permissions
   ```
4. ✅ **Create firewall rule** for port 4840
5. ✅ **Install and start Windows Service**
6. ✅ **Create Start Menu shortcuts**
7. ✅ **Launch configuration wizard** on first run

**Configuration Wizard** (first run):
```
┌─ Welcome to CCStudio-Tunneler Setup ──────────┐
│                                                │
│ Step 1: Select OPC DA Server                  │
│                                                │
│ Detected servers on this machine:             │
│  ○ ASI.OPCServer.1                            │
│  ○ Matrikon.OPC.Simulation.1                  │
│  ○ Custom (enter ProgID)                      │
│                                                │
│ [Test Connection]  [Next >]                   │
└────────────────────────────────────────────────┘

┌─ Step 2: Browse and Select Tags ──────────────┐
│ Connected to: ASI.OPCServer.1                 │
│                                                │
│ ☑ Building_01 (152 tags)                      │
│   ☑ AHU_01 (24 tags)                          │
│   ☑ AHU_02 (24 tags)                          │
│   ☐ Chillers (48 tags)                        │
│                                                │
│ Selected: 48 tags                             │
│ [< Back]  [Next >]                            │
└────────────────────────────────────────────────┘

┌─ Step 3: Configure OPC UA Server ─────────────┐
│                                                │
│ Server Port: [4840___]                        │
│ Security:    [None ▼]                         │
│ Auth:        [☑] Allow Anonymous              │
│                                                │
│ Your Mango M2M2 should connect to:            │
│   opc.tcp://10.1.1.50:4840                    │
│                                                │
│ [< Back]  [Finish]                            │
└────────────────────────────────────────────────┘
```

**Post-Install Test**:
- Show "Test with UA Expert" button
- Launch UA Expert (if installed) or link to download
- Verify connection works before leaving site

**Why Critical**: Techs bill by the hour. Faster install = lower cost for customer.

---

### 🟡 HIGH PRIORITY - Important for Reliability

#### 7. Tag Update Rate Optimization
**Priority**: HIGH | **Effort**: 2 days

**Problem**: Different points need different update rates. Polling everything at 1 second wastes bandwidth.

**Solution**: Per-tag update rates
```json
{
  "TagMappings": [
    {
      "DaTagName": "OutdoorAirTemp",
      "UpdateRate": 60000,  // 1 minute (slow-changing)
      "Deadband": 0.5       // Only update if changed by 0.5°F
    },
    {
      "DaTagName": "HighTempAlarm",
      "UpdateRate": 1000,   // 1 second (critical alarm)
      "Deadband": 0         // Always update on change
    }
  ]
}
```

**Smart Defaults**:
- **Analog inputs** (temps, pressures): 30-60 seconds, deadband 1%
- **Digital status** (fan on/off): 5 seconds, on-change only
- **Alarms**: 1 second, always update
- **Setpoints**: 10 seconds

**Benefits**:
- Reduce network traffic to Mango
- Lower CPU usage on Tunneler
- Still get real-time data for critical points

---

#### 8. Detailed Logging for Troubleshooting
**Priority**: HIGH | **Effort**: 1-2 days

**Problem**: When things break, need detailed logs to diagnose.

**Enhanced Logging**:
```
[2025-10-21 10:35:42.123] [INFO] Starting CCStudio-Tunneler v1.0.0
[2025-10-21 10:35:42.456] [INFO] Loading configuration from C:\ProgramData\...
[2025-10-21 10:35:43.001] [INFO] Starting OPC UA Server on port 4840
[2025-10-21 10:35:43.500] [INFO] OPC UA Server started successfully
[2025-10-21 10:35:43.600] [INFO] Connecting to OPC DA: ASI.OPCServer.1 on localhost
[2025-10-21 10:35:44.200] [INFO] Connected to OPC DA server successfully
[2025-10-21 10:35:44.300] [INFO] Creating OPC DA group: CCStudioGroup (UpdateRate: 1000ms)
[2025-10-21 10:35:44.400] [INFO] Adding 247 tags to subscription
[2025-10-21 10:35:45.000] [INFO] Subscribed to 247 tags successfully
[2025-10-21 10:35:45.100] [INFO] Bridge is now operational
[2025-10-21 10:36:00.000] [DEBUG] Tag update: AHU_01.SupplyTemp = 72.5 (Good) → HVAC/AHU_01/SupplyTemp
[2025-10-21 10:36:00.050] [DEBUG] Tag update: AHU_01.ReturnTemp = 75.2 (Good) → HVAC/AHU_01/ReturnTemp
[2025-10-21 10:40:15.000] [INFO] OPC UA client connected: Mango M2M2 (10.2.2.100:52341)
[2025-10-21 10:40:16.000] [INFO] Client subscribed to 150 nodes
[2025-10-21 11:15:30.000] [WARN] OPC DA server disconnected (HRESULT: 0x800706BA - RPC server unavailable)
[2025-10-21 11:15:30.100] [INFO] Attempting to reconnect (attempt 1/∞)
[2025-10-21 11:15:35.100] [INFO] Reconnected to OPC DA server successfully
[2025-10-21 11:15:35.200] [INFO] Re-subscribed to 247 tags
```

**Configurable Levels**:
- **ERROR**: Only failures (production)
- **WARN**: Errors + warnings (default)
- **INFO**: General operations (troubleshooting)
- **DEBUG**: Detailed tag updates (commissioning)
- **TRACE**: Everything including COM calls (developer only)

**Log Rotation**:
- Max file size: 10 MB
- Keep last 7 days
- Compress old logs (gzip)

**Log Viewer in UI**:
- View last 1000 lines
- Filter by level
- Search for tag names
- Export to file

---

#### 9. Security Best Practices
**Priority**: HIGH | **Effort**: 2 days

**Problem**: IT departments require security. Default "allow anonymous" won't pass audit.

**OPC UA Security Profiles**:
```json
{
  "OpcUa": {
    "SecurityPolicies": [
      {
        "Mode": "None",
        "Enabled": true,  // For initial setup/testing
        "Description": "No encryption (local network only)"
      },
      {
        "Mode": "Sign",
        "Enabled": true,  // Message integrity
        "Description": "Signed messages (tampering prevention)"
      },
      {
        "Mode": "SignAndEncrypt",
        "Enabled": true,  // Full security
        "Description": "Encrypted messages (recommended for production)"
      }
    ],
    "Authentication": {
      "AllowAnonymous": false,  // Disable for production
      "Users": [
        {
          "Username": "mango",
          "Password": "hashed_password_here",
          "Role": "Operator"
        }
      ]
    }
  }
}
```

**Certificate Management**:
- Auto-generate self-signed cert on first run
- Store in `C:\ProgramData\DHC Automation\CCStudio-Tunneler\PKI\`
- Easy export for Mango trust
- Support for CA-signed certificates

**UI Helper**:
```
┌─ Security Setup ───────────────────────────────┐
│ Certificate:                                   │
│  ✓ Self-signed certificate created            │
│    Thumbprint: A1:B2:C3:D4:E5:F6...           │
│    Valid until: 2030-10-21                    │
│                                                │
│  [Export for Mango] [Use Custom Certificate]  │
│                                                │
│ Authentication:                                │
│  ○ Allow Anonymous (Testing only)             │
│  ● Username/Password                           │
│    Username: [mango___________]               │
│    Password: [**************_]               │
│                                                │
│ Security Mode:                                 │
│  ☑ None (local testing)                       │
│  ☑ Sign (message integrity)                   │
│  ☑ SignAndEncrypt (recommended)               │
└────────────────────────────────────────────────┘
```

**Quick Guide**: "Configuring Mango to Trust Certificate" (step-by-step PDF)

---

#### 10. Pre-configured Templates for Common Systems
**Priority**: MEDIUM | **Effort**: 1 day

**Problem**: Every ASI, Johnson Controls, Honeywell system has similar structure. Don't start from scratch.

**Templates**:

**ASI Controls (WebCTRL-based)**:
```json
{
  "Name": "ASI Controls - Typical HVAC",
  "OpcDa": {
    "ServerProgId": "ASI.OPCServer.1",
    "ServerHost": "localhost",
    "UpdateRate": 10000,
    "DeadBand": 1.0,
    "TagPatterns": [
      "*.AHU_*.SupplyAirTemp",
      "*.AHU_*.ReturnAirTemp",
      "*.AHU_*.SupplyFanSpeed",
      "*.AHU_*.SupplyFanStatus",
      "*.VAV_*.DamperPosition",
      "*.VAV_*.SpaceTemp",
      "*.Chiller_*.ChilledWaterTemp",
      "*.Boiler_*.HotWaterTemp"
    ]
  }
}
```

**Johnson Controls Metasys**:
```json
{
  "Name": "Johnson Controls Metasys",
  "OpcDa": {
    "ServerProgId": "JohnsonControls.Metasys.OPCServer",
    "UpdateRate": 5000,
    "TagPatterns": [
      "Site.*.*",  // Typical Metasys hierarchy
      "NAE*.*.*"   // Network Automation Engines
    ]
  }
}
```

**Honeywell EBI**:
```json
{
  "Name": "Honeywell EBI",
  "OpcDa": {
    "ServerProgId": "Honeywell.EBI.OPCServer",
    "UpdateRate": 10000
  }
}
```

**Siemens Desigo**:
```json
{
  "Name": "Siemens Desigo",
  "OpcDa": {
    "ServerProgId": "Siemens.Desigo.OPC",
    "UpdateRate": 5000
  }
}
```

**UI**: "Load Template" dropdown in configuration wizard

---

### 🟢 MEDIUM PRIORITY - Nice to Have

#### 11. Backup & Restore Configuration
**Priority**: MEDIUM | **Effort**: 1 day

**Problem**: Lose configuration if Windows crashes or reinstalled.

**Solution**:
```
File → Backup Configuration → Save to:
  • Local file (USB stick)
  • Network share
  • Cloud (OneDrive, Dropbox)

File → Restore Configuration → Load from file
```

**Auto-Backup**:
- Daily backup to `C:\ProgramData\...\Backups\`
- Keep last 7 days
- Include: config.json, certificates, tag mappings

---

#### 12. Remote Status Monitoring
**Priority**: MEDIUM | **Effort**: 2 days

**Problem**: Can't check status without RDP or being on-site.

**Solution**: Simple web status page

```
http://tunneler-pc:8080/status

┌─ CCStudio-Tunneler Status ────────────────────┐
│ Service: ● Running                             │
│ Uptime: 15 days, 8 hours                      │
│                                                │
│ OPC DA Servers:                               │
│  ● ASI.OPCServer.1          Connected         │
│    Tags: 247   Updates/sec: 12                │
│                                                │
│ OPC UA Server:                                │
│  ● Listening on port 4840                     │
│  ● Clients: 1 (Mango @ 10.2.2.100)           │
│                                                │
│ Last Error: None                              │
│ Memory: 84 MB   CPU: 2%                       │
└────────────────────────────────────────────────┘
```

**Read-Only**: Can't change settings, just view status

---

#### 13. Bulk Import/Export Tag Mappings
**Priority**: MEDIUM | **Effort**: 1 day

**Problem**: Mapping 500+ tags one-by-one is tedious.

**Solution**: CSV import/export

**Export to CSV**:
```csv
DATagName,UANodeName,UpdateRate,Deadband,Enabled
Building_01.AHU_01.SupplyTemp,HVAC/AHU_01/SupplyTemp,10000,0.5,true
Building_01.AHU_01.ReturnTemp,HVAC/AHU_01/ReturnTemp,10000,0.5,true
Building_01.AHU_01.FanSpeed,HVAC/AHU_01/FanSpeed,5000,1.0,true
...
```

**Edit in Excel**:
- Bulk rename
- Copy/paste patterns
- Add scaling factors

**Import CSV**:
- Validate before importing
- Preview changes
- Merge or replace existing

**Use Case**: Clone configuration to similar building

---

#### 14. Performance Dashboard
**Priority**: MEDIUM | **Effort**: 2 days

**Problem**: Want to know if bridge is keeping up or struggling.

**Metrics**:
```
┌─ Performance ──────────────────────────────────┐
│ Messages Processed:                            │
│   Today: 1,247,893                            │
│   Per second: 47 (avg)  52 (current)          │
│                                                │
│ Latency (DA to UA):                           │
│   Average: 12 ms                              │
│   95th percentile: 45 ms                      │
│   99th percentile: 120 ms                     │
│                                                │
│ Tag Updates:                                   │
│   Top 5 most active:                          │
│    1. AHU_01.SupplyTemp     (120 upd/min)     │
│    2. OutdoorTemp           (60 upd/min)      │
│    3. ChillerStatus         (30 upd/min)      │
│                                                │
│ Errors (last 24h): 0                          │
│ Reconnections: 0                              │
└────────────────────────────────────────────────┘
```

**Alerts**:
- Warn if latency > 500ms
- Warn if error rate > 1%
- Warn if memory growing (leak)

---

#### 15. Network Diagnostics
**Priority**: MEDIUM | **Effort**: 1 day

**Problem**: "It's not working" - is it network, DCOM, firewall?

**Built-in Tools**:
```
Tools → Diagnostics

┌─ Network Diagnostics ─────────────────────────┐
│ Test OPC DA Connection:                       │
│  ○ Ping localhost                  [Run]      │
│  ○ Resolve ProgID in registry     [Run]      │
│  ○ Connect to OPC server           [Run]      │
│  ○ Browse tag tree                 [Run]      │
│  ○ Read sample values              [Run]      │
│                                                │
│ Test OPC UA Server:                           │
│  ○ Port 4840 listening             [Run]      │
│  ○ Firewall allows connections     [Run]      │
│  ○ Connect locally with UA Expert  [Run]      │
│  ○ Generate test certificate       [Run]      │
│                                                │
│ [Run All Tests]  [Export Report]              │
└────────────────────────────────────────────────┘
```

**Export Report**: PDF or text file to attach to support ticket

---

### 🔵 LOW PRIORITY - Future Enhancements

#### 16. Tag Aliasing
**Priority**: LOW | **Effort**: 1 day

**Problem**: ASI tag names are ugly: `Building_01.FCU_AHU_01.SAT`

**Want**: `Building1/AHU1/SupplyAirTemp`

**Solution**: Friendly names
```json
{
  "TagMappings": [
    {
      "DaTagName": "Building_01.FCU_AHU_01.SAT",
      "UaNodeName": "Building1/AHU1/SupplyAirTemp",
      "DisplayName": "Supply Air Temperature - AHU 1",
      "Description": "Supply air temp sensor for AHU-1 serving floors 1-3"
    }
  ]
}
```

---

#### 17. Redundant Tunneler Instances
**Priority**: LOW | **Effort**: 3-5 days

**Problem**: Single point of failure.

**Solution**: Active-passive failover

```
Primary Tunneler (10.1.1.50:4840)  ─┐
                                    ├─→ OPC DA Server
Backup Tunneler  (10.1.1.51:4840)  ─┘

Mango connects to: opc.tcp://10.1.1.50:4840
If primary fails, manually change to: opc.tcp://10.1.1.51:4840
```

**Future**: Automatic failover (complex, needs VIP or load balancer)

---

#### 18. Tag Value Caching
**Priority**: LOW | **Effort**: 1 day

**Problem**: If OPC DA goes offline, Tunneler shows "Bad" quality for all tags.

**Solution**: Serve last-known-good values with "Uncertain" quality

```csharp
var cache = new TagValueCache(retentionTime: TimeSpan.FromMinutes(5));

// On DA update
cache.Update("AHU_01.SupplyTemp", new TagValue { Value = 72.5, Quality = Good });

// On DA disconnect
var cachedValue = cache.Get("AHU_01.SupplyTemp");
cachedValue.Quality = Uncertain; // Mark as stale
return cachedValue; // Serve to OPC UA clients
```

**Benefit**: Mango still sees data during brief disconnects (graceful degradation)

---

## Summary: What Matters Most

### For Field Technicians:
1. **Easy Installation** - MSI that just works, wizard setup
2. **Tag Browser** - Must see ASI tag tree and select tags easily
3. **Templates** - Pre-configured for ASI, JCI, Honeywell
4. **Health Monitoring** - Know if it's working (green/red icon)
5. **Auto-Reconnect** - Recover from failures without manual intervention

### For System Reliability:
1. **Multi-Server Support** - One bridge for whole building
2. **Quality Mapping** - Preserve Good/Bad/Uncertain from OPC DA
3. **Data Type Handling** - Handle floats, ints, bools, strings correctly
4. **Detailed Logging** - Troubleshoot when issues occur
5. **Update Rate Optimization** - Don't overwhelm network

### For IT/Security:
1. **Security Modes** - Sign and encrypt for production
2. **Authentication** - No anonymous access
3. **Certificate Management** - Easy trust setup for Mango
4. **Audit Logging** - Who changed what when
5. **Firewall Configuration** - Clear documentation

---

## Implementation Roadmap

### Phase 1 (v1.0 - Production Ready) - 2-3 weeks
- ✅ Robust Tag Browser (COM-based hierarchical browsing)
- ✅ Multi-Server Support
- ✅ Auto-Reconnect with health monitoring
- ✅ Quality and data type mapping
- ✅ MSI Installer with wizard
- ✅ Templates for common systems
- ✅ Detailed logging

### Phase 2 (v1.1 - Reliability) - 1-2 weeks
- ✅ Update rate per tag
- ✅ Security best practices (username/password default)
- ✅ Certificate management UI
- ✅ Backup/restore configuration
- ✅ Performance dashboard

### Phase 3 (v1.2 - Operations) - 1-2 weeks
- ✅ Remote status monitoring (web page)
- ✅ Bulk CSV import/export
- ✅ Network diagnostics tools
- ✅ Tag value caching

### Phase 4 (v2.0 - Advanced) - Future
- Tag aliasing for friendly names
- Redundant instances
- Advanced DCOM troubleshooting tools
- Integration testing suite

---

## Testing Checklist for Mango Integration

Before deploying to production:

### Installation Testing
- [ ] Install on clean Windows 10 Pro VM
- [ ] Verify .NET runtime auto-installs
- [ ] Verify OPC Core Components detected/installed
- [ ] Verify Windows Service starts automatically
- [ ] Verify firewall rule created

### OPC DA Testing
- [ ] Connect to ASI Controls server
- [ ] Browse full tag hierarchy
- [ ] Subscribe to 100+ tags
- [ ] Verify tag values updating
- [ ] Verify quality codes (Good/Bad/Uncertain)
- [ ] Test write to setpoint tag
- [ ] Test auto-reconnect (stop/start OPC DA server)

### OPC UA Testing
- [ ] Connect with UA Expert client
- [ ] Browse exposed tag tree
- [ ] Subscribe to tags in UA Expert
- [ ] Verify values match OPC DA
- [ ] Verify quality and timestamps preserved
- [ ] Test write from UA Expert
- [ ] Test security modes (None, Sign, SignAndEncrypt)
- [ ] Test username/password authentication

### Mango M2M2 Integration
- [ ] Add OPC UA data source in Mango
- [ ] Configure endpoint: `opc.tcp://[tunneler-ip]:4840`
- [ ] Import certificate to Mango trust
- [ ] Browse tags from Mango
- [ ] Create data points in Mango
- [ ] Verify values updating in Mango
- [ ] Test writing to tags from Mango
- [ ] Create point hierarchy in Mango
- [ ] Setup alarms in Mango (high/low limits)
- [ ] Let run for 24 hours - verify stability

### Performance Testing
- [ ] 100 tags @ 10 second update rate
- [ ] 500 tags @ 30 second update rate
- [ ] Monitor CPU usage (should be < 5%)
- [ ] Monitor memory (should be stable, < 200 MB)
- [ ] Monitor network bandwidth
- [ ] Test with multiple Mango instances connected
- [ ] Run for 7 days - verify no memory leaks

### Failure Testing
- [ ] Stop OPC DA server - verify auto-reconnect
- [ ] Disconnect network cable - verify recovery
- [ ] Kill Tunneler service - verify Windows restarts it
- [ ] Reboot Windows - verify service starts on boot
- [ ] Fill disk with logs - verify log rotation works

---

## Documentation Needed

### For Installation Techs:
1. **Quick Start Guide** (2 pages)
   - Install MSI
   - Run wizard
   - Connect Mango
   - Done!

2. **ASI Controls Integration Guide** (5 pages)
   - DCOM configuration for ASI
   - Common tag patterns
   - Typical update rates
   - Troubleshooting ASI connection

3. **Mango M2M2 Integration Guide** (10 pages)
   - Add OPC UA data source
   - Certificate trust
   - Tag discovery
   - Creating point hierarchy
   - Best practices

### For IT/Security:
1. **Security Hardening Guide**
   - Disable anonymous access
   - Configure certificate-based auth
   - Firewall rules
   - DCOM security for OPC DA
   - Network segmentation

### For Support:
1. **Troubleshooting Guide**
   - Common errors and solutions
   - Log analysis
   - Network diagnostics
   - Performance tuning

---

**Next Steps**:
1. Prioritize Phase 1 features
2. Focus on robust OPC DA browsing (biggest gap)
3. Test with real ASI Controls system
4. Get feedback from field techs

---

**Document Version**: 1.0
**Focus**: Mango M2M2 to ASI Controls integration
**Last Updated**: 2025-10-21
