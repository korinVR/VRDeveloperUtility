# VR Developer Utility

A small Windows utility for VR development workflows.

## Features

- Detects the currently running ADB server and uses that `adb.exe`.
- Launches and stops the configured Android package through ADB.
- Reboots the connected Meta Quest through ADB.
- Launches the Meta Horizon Link desktop app.
- Restarts `OVRService` without opening a PowerShell console.
- Opens Oculus Debug Tool.

## Build

```powershell
dotnet build .\VRDeveloperUtility.csproj -c Release
```

```powershell
dotnet publish .\VRDeveloperUtility.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## MSI Installer

Build a self-contained Windows MSI installer:

```powershell
.\scripts\build-installer.ps1
```

The MSI package is written to:

```text
dist\installer\VRDeveloperUtilitySetup.msi
```

The installer copies the app to `%LOCALAPPDATA%\Programs\VRDeveloperUtility`, creates a Start Menu shortcut, and registers an uninstall entry for the current user.
