# CCStudio-Tunneler Tray App - Quick Start Guide

## What is This?

The **CCStudio-Tunneler Tray App** is a Windows system tray application that makes it **dead simple** to configure and manage your OPC DA to OPC UA bridge.

**No more editing JSON files!** Just:
1. Right-click the tray icon
2. Select your OPC DA server from the dropdown
3. Test the connection
4. Save and start the tunnel

## Features

✅ **Auto-discovers OPC DA servers** on your PC
✅ **One-click connection testing**
✅ **Visual configuration** - no JSON editing needed
✅ **Service management** - start/stop/restart from the tray
✅ **Real-time status** monitoring
✅ **Access logs** with one click

---

## Installation & First Run

### Step 1: Build the Application

Open **PowerShell** or **Command Prompt** in the project directory and run:

```powershell
cd src\CCStudio.Tunneler.TrayApp
dotnet build
```

### Step 2: Run the Tray App

```powershell
dotnet run
```

Or build and run the release version:

```powershell
dotnet build -c Release
.\bin\Release\net8.0-windows\CCStudio.Tunneler.TrayApp.exe
```

### Step 3: Look for the Tray Icon

The app runs in your **system tray** (bottom-right corner of your taskbar). Look for the CCStudio-Tunneler icon.

---

## How to Use

### Configure Your OPC DA Server

1. **Right-click** the tray icon
2. Select **"Configure..."**
3. In the **"OPC DA Source"** tab:
   - **Server ProgID**: The dropdown will auto-populate with discovered OPC DA servers
     - Select your **ASI Controls** server from the list
     - Or manually enter the ProgID if it's not listed
   - **Server Host**: Enter `localhost` (or the IP of the remote PC)
   - **Update Rate**: Default is 5000ms (5 seconds)
   - **Auto-discover tags**: Leave this **checked** (recommended)
4. Click **"Test Connection"** to verify it works
5. Switch to the **"OPC UA Server"** tab:
   - **Server Port**: Default is `4840`
   - **Endpoint URL**: Change `localhost` to your PC's IP if Mango is remote
     - Example: `opc.tcp://192.168.1.100:4840`
6. Click **"Save"**

### Start the Tunnel

1. Right-click the tray icon
2. Select **Service → Start Service**
3. Wait for the notification: "Service Started"

### Connect from Mango M2M2

1. In Mango, create a new **OPC UA Data Source**
2. Endpoint URL: `opc.tcp://YOUR-PC-IP:4840`
3. Security: **None** (anonymous)
4. **Browse** the tags and add them to Mango
5. Done! No tag mapping needed - the tunnel exposes everything.

---

## Tray Menu Reference

### Main Menu

- **Configure...** - Opens the configuration window
- **View Status** - Shows connection status, tag count, and errors
- **Service**
  - **Start Service** - Starts the OPC DA to OPC UA bridge
  - **Stop Service** - Stops the bridge
  - **Restart Service** - Restarts the bridge (apply config changes)
- **View Logs** - Opens the log folder in File Explorer
- **Open Config Folder** - Opens the config directory
- **About** - Shows version and company info
- **Exit** - Closes the tray app (service keeps running)

---

## Troubleshooting

### "OPC Server ProgID not found"

- Make sure your OPC DA server is installed and running
- Try manually entering the ProgID
- Check if OPC Core Components are installed

### "Connection Failed" during test

- Verify the OPC DA server is running
- Check that you're using the correct ProgID
- For remote servers, verify firewall settings
- Ensure DCOM permissions are configured correctly

### Tray icon doesn't appear

- Check Task Manager for `CCStudio.Tunneler.TrayApp.exe`
- Try restarting the app
- Check Windows notification area settings

### Service won't start

- Run the tray app as **Administrator**
- Check logs in: `C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs\`
- Verify configuration file is valid

### Tags don't show up in Mango

- Verify "Auto-discover tags" is enabled
- Check service status (should be "Running")
- Test connection to OPC DA server
- Check logs for subscription errors

---

## Advanced Features

### Manual Tag Mapping (Optional)

If you want to **rename tags** or apply **transformations**, use the **"Tag Mapping"** tab:

1. Click **"Browse DA Tags"** to see available tags (when implemented)
2. Add rows to the grid:
   - **OPC DA Tag**: Original tag name from the DA server
   - **OPC UA Node**: New name you want in OPC UA
   - **Access**: Read, Write, or ReadWrite
   - **Units**: Engineering units (optional)
3. Click **"Save"**

### Remote OPC DA Servers

To connect to an OPC DA server on another PC:

1. Enter the **IP address or hostname** in "Server Host"
2. Ensure **DCOM** is configured correctly:
   - Run `dcomcnfg` on both machines
   - Configure security permissions for OPC servers
   - Open firewall ports (usually 135, 4840)

### Logging Configuration

In the **"Logging"** tab, you can:
- Change log level (Verbose, Debug, Information, Warning, Error)
- Set log retention days
- Configure max file size
- Enable/disable console logging

---

## Config File Location

The tray app automatically generates the config file at:

```
C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json
```

You can still manually edit this file if needed, but **it's not recommended** - use the tray app instead!

---

## Why This is Better Than Manual Config Files

### Before (Manual Config):
1. Open JSON file in Notepad
2. Find and enter ProgID (hope you typed it correctly)
3. Manually edit IP addresses
4. Save file
5. Restart service via command line
6. Check logs to see if it worked
7. Repeat if something went wrong

### Now (Tray App):
1. Right-click → Configure
2. Select server from dropdown
3. Test connection (button)
4. Save
5. Restart (button)
6. ✅ Done!

---

## What's Next?

Once the tunnel is running, you can:

1. **Browse tags** from Mango M2M2 OPC UA client
2. **Add data points** in Mango by browsing the OPC UA address space
3. **Monitor status** via the tray app's Status window
4. **View logs** if you encounter any issues

Enjoy your simplified OPC DA to OPC UA bridging experience!

---

## Need Help?

- Check the logs: Right-click tray icon → "View Logs"
- Test your connection: Configure → Test Connection button
- View status: Right-click → "View Status"
- See detailed docs: `TESTING_GUIDE.md` and `READY_FOR_TESTING.md`

---

**Made with ❤️ by DHC Automation and Controls**
