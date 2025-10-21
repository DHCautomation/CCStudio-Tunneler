# Build Instructions

## Quick Build

### Prerequisites

1. **Visual Studio 2022** or later (Community Edition works)
   - Workload: ".NET Desktop Development"
   - Workload: "Windows Application Development"

2. **.NET 8.0 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Click "x64" under SDK 8.0.121 (Windows)

3. **Git** for version control
   - Download: https://git-scm.com/

### Clone and Build

```bash
# Clone repository
git clone https://github.com/dhcautomation/CCStudio-Tunneler.git
cd CCStudio-Tunneler

# Restore NuGet packages
dotnet restore

# Build solution
dotnet build -c Release

# Or build specific project
dotnet build src/CCStudio.Tunneler.Service/CCStudio.Tunneler.Service.csproj -c Release
```

### Using Visual Studio

1. Open `CCStudio-Tunneler.sln`
2. Select "Release" configuration
3. Build → Build Solution (Ctrl+Shift+B)

## Detailed Build Steps

### Build Output Locations

After building, output will be in:

```
src/CCStudio.Tunneler.Core/bin/Release/net8.0/
src/CCStudio.Tunneler.Service/bin/Release/net8.0-windows/
src/CCStudio.Tunneler.TrayApp/bin/Release/net8.0-windows/
```

### Building Each Component

#### Core Library

```bash
cd src/CCStudio.Tunneler.Core
dotnet build -c Release
```

#### Windows Service

```bash
cd src/CCStudio.Tunneler.Service
dotnet build -c Release
```

#### Tray Application

```bash
cd src/CCStudio.Tunneler.TrayApp
dotnet build -c Release
```

## Publishing for Deployment

### Self-Contained Deployment (includes .NET runtime)

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Windows x86
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true
```

### Framework-Dependent Deployment (requires .NET 6.0 installed)

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Output will be in:
```
src/[ProjectName]/bin/Release/net8.0-windows/win-x64/publish/
```

## Creating Distribution Packages

### Portable ZIP Package

```bash
# Build and publish
dotnet publish -c Release -r win-x64 --self-contained true

# Create distribution folder
mkdir -p dist/portable
cp -r src/CCStudio.Tunneler.Service/bin/Release/net8.0-windows/win-x64/publish/* dist/portable/Service/
cp -r src/CCStudio.Tunneler.TrayApp/bin/Release/net8.0-windows/win-x64/publish/* dist/portable/TrayApp/
cp images/* dist/portable/
cp README.md LICENSE dist/portable/

# Create ZIP
cd dist
zip -r CCStudio-Tunneler-Portable-v1.0.0.zip portable/
```

### MSI Installer (WiX Toolset)

1. **Install WiX Toolset**:
   - Download from https://wixtoolset.org/
   - Or install via Visual Studio extension

2. **Create WiX Project** (if not already created):

```xml
<!-- Product.wxs -->
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="CCStudio-Tunneler" Language="1033" Version="1.0.0.0"
           Manufacturer="DHC Automation and Controls" UpgradeCode="PUT-GUID-HERE">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />

    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    <MediaTemplate EmbedCab="yes" />

    <Feature Id="ProductFeature" Title="CCStudio-Tunneler" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
      <ComponentGroupRef Id="ServiceComponents" />
    </Feature>
  </Product>

  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="DHC Automation">
          <Directory Id="PRODUCTFOLDER" Name="CCStudio-Tunneler" />
        </Directory>
      </Directory>
    </Directory>
  </Fragment>

  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="PRODUCTFOLDER">
      <!-- Add files here -->
    </ComponentGroup>
  </Fragment>
</Wix>
```

3. **Build Installer**:

```bash
candle Product.wxs
light -out CCStudio-Tunneler-Setup.msi Product.wixobj
```

## Development Build

### Debug Build

```bash
dotnet build -c Debug
```

### Running from Source

#### Service (Console Mode)

```bash
cd src/CCStudio.Tunneler.Service
dotnet run
```

#### Tray Application

```bash
cd src/CCStudio.Tunneler.TrayApp
dotnet run
```

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Project

```bash
dotnet test tests/CCStudio.Tunneler.Tests/CCStudio.Tunneler.Tests.csproj
```

### Run with Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore -c Release

    - name: Test
      run: dotnet test --no-build --verbosity normal

    - name: Publish
      run: dotnet publish -c Release -r win-x64 --self-contained true

    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: ccstudio-tunneler
        path: src/*/bin/Release/net8.0-windows/win-x64/publish/
```

## Troubleshooting Build Issues

### NuGet Restore Fails

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore with verbose logging
dotnet restore --verbosity detailed
```

### Build Errors

```bash
# Clean build
dotnet clean
dotnet build -c Release

# Or in Visual Studio
Build → Clean Solution
Build → Rebuild Solution
```

### Missing SDK

```bash
# Check installed SDKs
dotnet --list-sdks

# Should show 8.0.xxx or higher
# Install .NET 8.0 SDK if missing
# Download from https://dotnet.microsoft.com/download/dotnet/8.0
```

### WPF Build Errors

Ensure you have:
- Windows SDK installed
- "Windows Application Development" workload in Visual Studio

### Icon/Resource Errors

If images/icons are missing:

1. Ensure images are in the `images/` folder
2. Verify .csproj includes resources:
```xml
<ItemGroup>
  <Resource Include="..\..\images\*.png" />
</ItemGroup>
```

## Version Management

### Update Version Numbers

Update in all `.csproj` files:

```xml
<PropertyGroup>
  <Version>1.0.0</Version>
</PropertyGroup>
```

Also update:
- `docs/CHANGELOG.md`
- `README.md`
- `src/CCStudio.Tunneler.Core/Utilities/Constants.cs`

## Release Checklist

- [ ] Update version numbers
- [ ] Update CHANGELOG.md
- [ ] Run all tests
- [ ] Build Release configuration
- [ ] Test on clean Windows machine
- [ ] Create GitHub release
- [ ] Upload installers/packages
- [ ] Update documentation

---

**For build support: dev@dhcautomation.com**
