[CmdletBinding()]
param(
    [string] $GamePath,
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    [switch] $Install,
    [switch] $UpdateExisting
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "WarpBookmarks.csproj"
$ManifestPath = Join-Path $ProjectRoot "manifest.json"
$Manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json

function Find-GamePath {
    param([string] $RequestedPath)

    $candidates = @()
    if ($RequestedPath) {
        $candidates += $RequestedPath
    }
    if (${env:ProgramFiles(x86)}) {
        $candidates += Join-Path ${env:ProgramFiles(x86)} "Steam\steamapps\common\Stardew Valley"
    }
    if ($env:ProgramFiles) {
        $candidates += Join-Path $env:ProgramFiles "Steam\steamapps\common\Stardew Valley"
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        $gameAssembly = Join-Path $candidate "Stardew Valley.dll"
        $smapiAssembly = Join-Path $candidate "StardewModdingAPI.dll"
        if ((Test-Path $gameAssembly) -and (Test-Path $smapiAssembly)) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Could not find Stardew Valley.dll and StardewModdingAPI.dll. Pass -GamePath explicitly."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found. Install the .NET 6 SDK x64 and reopen PowerShell."
}

$sdkList = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0 -or -not ($sdkList | Where-Object { $_ -match '^6\.' })) {
    throw ".NET 6 SDK was not found."
}

$ResolvedGamePath = Find-GamePath $GamePath
Write-Host "Game path: $ResolvedGamePath"
Write-Host "Configuration: $Configuration"

& dotnet restore $ProjectFile "-p:GamePath=$ResolvedGamePath"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

& dotnet build $ProjectFile --configuration $Configuration --no-restore "-p:GamePath=$ResolvedGamePath"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$OutputPath = Join-Path $ProjectRoot "bin\$Configuration\net6.0"
$ModAssembly = Join-Path $OutputPath "WarpBookmarks.dll"
if (-not (Test-Path $ModAssembly)) {
    throw "Expected assembly was not found at '$ModAssembly'."
}
Write-Host "Build succeeded: $ModAssembly"

$ReleaseZip = Get-ChildItem (Join-Path $ProjectRoot "bin") -Recurse -Filter "*.zip" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($ReleaseZip) {
    Write-Host "Release package: $($ReleaseZip.FullName)"
}

if ($Install) {
    $ModsPath = Join-Path $ResolvedGamePath "Mods"
    $TargetPath = Join-Path $ModsPath "WarpBookmarks"
    if (-not (Test-Path $ModsPath)) {
        throw "SMAPI Mods folder not found at '$ModsPath'."
    }

    if (Test-Path $TargetPath) {
        $InstalledManifestPath = Join-Path $TargetPath "manifest.json"
        if (-not (Test-Path $InstalledManifestPath)) {
            throw "Refusing to update a folder without manifest.json."
        }
        $InstalledManifest = Get-Content $InstalledManifestPath -Raw | ConvertFrom-Json
        if ($InstalledManifest.UniqueID -ne $Manifest.UniqueID) {
            throw "Refusing to update target with UniqueID '$($InstalledManifest.UniqueID)'."
        }
    }
    else {
        New-Item -ItemType Directory -Path $TargetPath | Out-Null
    }

    Copy-Item $ModAssembly $TargetPath -Force
    $Symbols = Join-Path $OutputPath "WarpBookmarks.pdb"
    if (Test-Path $Symbols) {
        Copy-Item $Symbols $TargetPath -Force
    }
    Copy-Item $ManifestPath $TargetPath -Force

    $SourceI18n = Join-Path $ProjectRoot "i18n"
    $TargetI18n = Join-Path $TargetPath "i18n"
    if (-not (Test-Path $TargetI18n)) {
        New-Item -ItemType Directory -Path $TargetI18n | Out-Null
    }
    Get-ChildItem $SourceI18n -File | Copy-Item -Destination $TargetI18n -Force
    Write-Host "Installed or updated Warp Bookmarks at: $TargetPath"
}

Write-Host "See docs\WINDOWS_RELEASE_TEST_$($Manifest.Version).md for the final release check."
