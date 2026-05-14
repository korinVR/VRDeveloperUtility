$ErrorActionPreference = "Stop"
if ($PSVersionTable.PSVersion.Major -ge 7) {
    $PSNativeCommandUseErrorActionPreference = $true
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProject = Join-Path $repoRoot "VRDeveloperUtility.csproj"
$wixProject = Join-Path $repoRoot "installer\VRDeveloperUtility.Installer.wixproj"
$publishDir = Join-Path $repoRoot "dist\app"
$installerDir = Join-Path $repoRoot "dist\installer"

New-Item -ItemType Directory -Force -Path $installerDir | Out-Null

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $installerDir) {
    Remove-Item -LiteralPath $installerDir -Recurse -Force
}

dotnet publish $appProject `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet build $wixProject `
    -c Release `
    -p:SourceDir="$publishDir" `
    -p:OutputPath="$installerDir\"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$msiPath = Join-Path $installerDir "VRDeveloperUtilitySetup.msi"
Write-Host "MSI created: $msiPath"
