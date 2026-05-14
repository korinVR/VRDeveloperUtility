# VR Developer Utility

A small Windows utility for VR development workflows.

## Features

- Detects the currently running ADB server and uses that `adb.exe`.
- Launches and stops the configured Android package through ADB.
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
