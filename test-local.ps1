# CCStudio-Tunneler Local Testing Script
# Quick test without installation

Write-Host "`nCCStudio-Tunneler - Local Testing" -ForegroundColor Green
Write-Host "===================================`n" -ForegroundColor Green

# Check .NET 8.0
Write-Host "Checking .NET 8.0..." -ForegroundColor Cyan
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET 8.0 SDK not found!" -ForegroundColor Red
    Write-Host "Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    pause
    exit 1
}
Write-Host "Found .NET version: $dotnetVersion" -ForegroundColor Green

# Build solution
Write-Host "`nBuilding solution..." -ForegroundColor Cyan
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Check errors above." -ForegroundColor Red
    pause
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green

# Display menu
Write-Host "`n================================" -ForegroundColor Cyan
Write-Host "What would you like to test?" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "1. Tray Application (UI/UX testing)"
Write-Host "2. Service (Console Mode)"
Write-Host "3. Both (Service + Tray App)"
Write-Host "4. Exit"
Write-Host ""

$choice = Read-Host "Enter choice (1-4)"

switch ($choice) {
    "1" {
        Write-Host "`nLaunching Tray Application..." -ForegroundColor Green
        $trayPath = "src\CCStudio.Tunneler.TrayApp\bin\Release\net8.0-windows"
        Start-Process -FilePath "$trayPath\CCStudio.Tunneler.TrayApp.exe" -WorkingDirectory $trayPath
        Write-Host "Tray app launched! Check your system tray." -ForegroundColor Green
    }
    "2" {
        Write-Host "`nStarting Service in Console Mode..." -ForegroundColor Green
        Write-Host "Press Ctrl+C to stop`n" -ForegroundColor Yellow
        $servicePath = "src\CCStudio.Tunneler.Service\bin\Release\net8.0-windows"
        Push-Location $servicePath
        .\CCStudio.Tunneler.Service.exe
        Pop-Location
    }
    "3" {
        Write-Host "`nLaunching both Service and Tray App..." -ForegroundColor Green

        # Start service in background
        $servicePath = "src\CCStudio.Tunneler.Service\bin\Release\net8.0-windows"
        $serviceJob = Start-Process -FilePath "$servicePath\CCStudio.Tunneler.Service.exe" -WorkingDirectory $servicePath -PassThru
        Write-Host "Service started (PID: $($serviceJob.Id))" -ForegroundColor Green

        Start-Sleep -Seconds 2

        # Start tray app
        $trayPath = "src\CCStudio.Tunneler.TrayApp\bin\Release\net8.0-windows"
        Start-Process -FilePath "$trayPath\CCStudio.Tunneler.TrayApp.exe" -WorkingDirectory $trayPath
        Write-Host "Tray app launched!" -ForegroundColor Green

        Write-Host "`nPress any key to stop service and exit..."
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

        Stop-Process -Id $serviceJob.Id -Force -ErrorAction SilentlyContinue
        Write-Host "Service stopped." -ForegroundColor Yellow
    }
    "4" {
        Write-Host "Exiting..." -ForegroundColor Yellow
        exit 0
    }
    default {
        Write-Host "Invalid choice!" -ForegroundColor Red
    }
}

Write-Host "`nTest complete!" -ForegroundColor Green
