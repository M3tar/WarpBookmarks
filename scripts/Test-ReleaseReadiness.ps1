[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Manifest = Get-Content (Join-Path $ProjectRoot "manifest.json") -Raw | ConvertFrom-Json
$ProjectXml = [xml](Get-Content (Join-Path $ProjectRoot "WarpBookmarks.csproj") -Raw)
$ProjectVersion = [string]$ProjectXml.Project.PropertyGroup.Version
$PackageSource = Get-Content (Join-Path $PSScriptRoot "Package-Windows.ps1") -Raw
$Failures = @()

if ($Manifest.Version -ne "1.0.1") {
    $Failures += "manifest.json is not set to release version 1.0.1."
}
if ($Manifest.UniqueID -ne "Mercury.WarpBookmarks") {
    $Failures += "The stable Mod UniqueID changed unexpectedly."
}
if ($ProjectVersion -ne $Manifest.Version) {
    $Failures += "WarpBookmarks.csproj version '$ProjectVersion' doesn't match manifest version '$($Manifest.Version)'."
}

if ($PackageSource -notmatch '(?m)^Add-Type -AssemblyName System\.IO\.Compression\r?$') {
    $Failures += "Package-Windows.ps1 must explicitly load System.IO.Compression for Windows PowerShell 5.1 before using ZipArchive."
}
if ($PackageSource -notmatch '(?m)^Add-Type -AssemblyName System\.IO\.Compression\.FileSystem\r?$') {
    $Failures += "Package-Windows.ps1 must load System.IO.Compression.FileSystem for ZipFile helpers."
}

$RequiredPackageEntries = @(
    "WarpBookmarks/WarpBookmarks.dll",
    "WarpBookmarks/manifest.json",
    "WarpBookmarks/i18n/default.json",
    "WarpBookmarks/i18n/zh.json"
)
foreach ($Entry in $RequiredPackageEntries) {
    $EscapedEntry = [Regex]::Escape($Entry.Replace("/", "\"))
    if ($PackageSource -notmatch $EscapedEntry -and $PackageSource -notmatch [Regex]::Escape((Split-Path $Entry -Leaf))) {
        $Failures += "Packaging workflow doesn't account for '$Entry'."
    }
}

$RequiredDocuments = @(
    "README.md",
    "README.en.md",
    "CHANGELOG.md",
    "docs\NEXUS_DESCRIPTION.md",
    "docs\NEXUS_DESCRIPTION.bbcode.txt",
    "docs\NEXUS_RELEASE_CHECKLIST.md",
    "docs\NEXUS_UPDATE_1.0.1.md",
    "docs\NEXUS_RELEASE_CHECKLIST_1.0.1.md",
    "docs\WINDOWS_RELEASE_TEST_1.0.1.md"
)
foreach ($Document in $RequiredDocuments) {
    if (-not (Test-Path (Join-Path $ProjectRoot $Document))) {
        $Failures += "Release document is missing: $Document"
    }
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "Release readiness guard failed with $($Failures.Count) error(s)."
}

Write-Host "Release readiness guards passed: $ProjectRoot"
