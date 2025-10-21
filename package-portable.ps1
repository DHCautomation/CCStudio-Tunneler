# CCStudio-Tunneler Portable Package Builder
# Run this from PowerShell in Windows

param(
    [string]$OutputPath = ".\dist\portable"
)

Write-Host "Building CCStudio-Tunneler Portable Package..." -ForegroundColor Green

# Build the solution
Write-Host "`nStep 1: Building solution..." -ForegroundColor Cyan
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Create output directory
Write-Host "`nStep 2: Creating output directory..." -ForegroundColor Cyan
if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputPath | Out-Null

# Copy Service
Write-Host "`nStep 3: Copying Service..." -ForegroundColor Cyan
$servicePath = "src\CCStudio.Tunneler.Service\bin\Release\net8.0-windows"
Copy-Item -Path $servicePath -Destination "$OutputPath\Service" -Recurse

# Copy Tray App
Write-Host "`nStep 4: Copying Tray App..." -ForegroundColor Cyan
$trayPath = "src\CCStudio.Tunneler.TrayApp\bin\Release\net8.0-windows"
Copy-Item -Path $trayPath -Destination "$OutputPath\TrayApp" -Recurse

# Copy images
Write-Host "`nStep 5: Copying images..." -ForegroundColor Cyan
Copy-Item -Path "images" -Destination "$OutputPath\images" -Recurse

# Copy documentation
Write-Host "`nStep 6: Copying documentation..." -ForegroundColor Cyan
Copy-Item -Path "README.md" -Destination "$OutputPath\"
Copy-Item -Path "LICENSE" -Destination "$OutputPath\"
Copy-Item -Path "docs" -Destination "$OutputPath\docs" -Recurse

# Create installation script
Write-Host "`nStep 7: Creating installation scripts..." -ForegroundColor Cyan

$installScript = @'
# CCStudio-Tunneler Installation Script
# Run as Administrator

Write-Host "CCStudio-Tunneler Installation" -ForegroundColor Green
Write-Host "================================`n" -ForegroundColor Green

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

# Get installation path
$defaultPath = "C:\Program Files\DHC Automation\CCStudio-Tunneler"
$installPath = Read-Host "Installation path [$defaultPath]"
if ([string]::IsNullOrWhiteSpace($installPath)) {
    $installPath = $defaultPath
}

# Create installation directory
Write-Host "`nCreating installation directory..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $installPath -Force | Out-Null

# Copy files
Write-Host "Copying files..." -ForegroundColor Cyan
Copy-Item -Path "Service\*" -Destination "$installPath\Service" -Recurse -Force
Copy-Item -Path "TrayApp\*" -Destination "$installPath\TrayApp" -Recurse -Force
Copy-Item -Path "images" -Destination "$installPath\images" -Recurse -Force
Copy-Item -Path "docs" -Destination "$installPath\docs" -Recurse -Force

# Install service
Write-Host "`nInstalling Windows Service..." -ForegroundColor Cyan
$serviceName = "CCStudioTunneler"
$serviceExists = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($serviceExists) {
    Write-Host "Service already exists. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $serviceName
    Start-Sleep -Seconds 2
}

$servicePath = "`"$installPath\Service\CCStudio.Tunneler.Service.exe`""
sc.exe create $serviceName binPath= $servicePath start= auto DisplayName= "CCStudio-Tunneler Service"
sc.exe description $serviceName "OPC DA to OPC UA Bridge by DHC Automation and Controls"

# Add firewall rule
Write-Host "`nConfiguring firewall..." -ForegroundColor Cyan
$firewallRule = Get-NetFirewallRule -DisplayName "CCStudio-Tunneler" -ErrorAction SilentlyContinue
if (-not $firewallRule) {
    New-NetFirewallRule -DisplayName "CCStudio-Tunneler" -Direction Inbound -Protocol TCP -LocalPort 4840 -Action Allow | Out-Null
    Write-Host "Firewall rule created for port 4840" -ForegroundColor Green
}

# Create Start Menu shortcut
Write-Host "`nCreating Start Menu shortcut..." -ForegroundColor Cyan
$startMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\DHC Automation"
New-Item -ItemType Directory -Path $startMenuPath -Force | Out-Null

$WScriptShell = New-Object -ComObject WScript.Shell
$shortcut = $WScriptShell.CreateShortcut("$startMenuPath\CCStudio-Tunneler.lnk")
$shortcut.TargetPath = "$installPath\TrayApp\CCStudio.Tunneler.TrayApp.exe"
$shortcut.WorkingDirectory = "$installPath\TrayApp"
$shortcut.IconLocation = "$installPath\images\CCStudio-Tunneler.ico"
$shortcut.Description = "CCStudio-Tunneler OPC DA to OPC UA Bridge"
$shortcut.Save()

# Start service
Write-Host "`nStarting service..." -ForegroundColor Cyan
Start-Service -Name $serviceName

# Launch tray app
Write-Host "`nLaunching tray application..." -ForegroundColor Cyan
Start-Process "$installPath\TrayApp\CCStudio.Tunneler.TrayApp.exe"

Write-Host "`n================================" -ForegroundColor Green
Write-Host "Installation Complete!" -ForegroundColor Green
Write-Host "================================`n" -ForegroundColor Green
Write-Host "Service Status: " -NoNewline
$serviceStatus = (Get-Service -Name $serviceName).Status
if ($serviceStatus -eq "Running") {
    Write-Host "Running" -ForegroundColor Green
} else {
    Write-Host $serviceStatus -ForegroundColor Yellow
}
Write-Host "`nTray application launched in system tray"
Write-Host "Documentation: $installPath\docs"
Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
'@

Set-Content -Path "$OutputPath\Install.ps1" -Value $installScript

# Create uninstall script
$uninstallScript = @'
# CCStudio-Tunneler Uninstallation Script
# Run as Administrator

Write-Host "CCStudio-Tunneler Uninstallation" -ForegroundColor Yellow
Write-Host "==================================`n" -ForegroundColor Yellow

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    pause
    exit 1
}

$serviceName = "CCStudioTunneler"

# Stop and remove service
Write-Host "Stopping and removing service..." -ForegroundColor Cyan
Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
sc.exe delete $serviceName

# Remove firewall rule
Write-Host "Removing firewall rule..." -ForegroundColor Cyan
Remove-NetFirewallRule -DisplayName "CCStudio-Tunneler" -ErrorAction SilentlyContinue

# Remove Start Menu shortcut
Write-Host "Removing shortcuts..." -ForegroundColor Cyan
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\DHC Automation\CCStudio-Tunneler.lnk" -ErrorAction SilentlyContinue

Write-Host "`nUninstallation complete!" -ForegroundColor Green
Write-Host "Note: Installation files and configuration have been preserved."
Write-Host "To remove completely, delete the installation folder manually."
Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
'@

Set-Content -Path "$OutputPath\Uninstall.ps1" -Value $uninstallScript

# Create README
$readmeContent = @"
# CCStudio-Tunneler Portable Package

## Requirements

- Windows 10/11 or Windows Server 2016+
- .NET 8.0 Runtime: https://dotnet.microsoft.com/download/dotnet/8.0
- Administrator privileges for installation

## Quick Start

### Installation

1. Right-click **Install.ps1** and select **"Run with PowerShell as Administrator"**
2. Follow the prompts
3. The service will start automatically
4. The tray application will launch

### Manual Running (No Installation)

**Run Tray App:**
```
TrayApp\CCStudio.Tunneler.TrayApp.exe
```

**Run Service (Console Mode):**
```
Service\CCStudio.Tunneler.Service.exe
```

### Uninstallation

Right-click **Uninstall.ps1** and select **"Run with PowerShell as Administrator"**

## Documentation

See the **docs** folder for complete documentation:
- docs/UserGuide.md - Complete user guide
- docs/DeveloperGuide.md - Developer documentation

## Default Configuration

Configuration is stored at:
```
C:\ProgramData\DHC Automation\CCStudio-Tunneler\config.json
```

Logs are stored at:
```
C:\ProgramData\DHC Automation\CCStudio-Tunneler\Logs
```

## Support

- Email: support@dhcautomation.com
- GitHub: https://github.com/dhcautomation/ccstudio-tunneler

---
CCStudio-Tunneler v1.0.0
DHC Automation and Controls
"@

Set-Content -Path "$OutputPath\README.txt" -Value $readmeContent

# Create ZIP file
Write-Host "`nStep 8: Creating ZIP package..." -ForegroundColor Cyan
$zipPath = "dist\CCStudio-Tunneler-Portable-v1.0.0.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path "$OutputPath\*" -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "`n================================" -ForegroundColor Green
Write-Host "Package Created Successfully!" -ForegroundColor Green
Write-Host "================================`n" -ForegroundColor Green
Write-Host "Output Directory: $OutputPath"
Write-Host "ZIP Package: $zipPath"
Write-Host "`nTo test on this machine:"
Write-Host "  cd $OutputPath"
Write-Host "  .\Install.ps1 (as Administrator)"
Write-Host "`nTo test on remote machine:"
Write-Host "  1. Copy $zipPath to remote PC"
Write-Host "  2. Extract ZIP"
Write-Host "  3. Run Install.ps1 as Administrator"
Write-Host ""
