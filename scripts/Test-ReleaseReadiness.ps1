[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Manifest = Get-Content (Join-Path $ProjectRoot "manifest.json") -Raw | ConvertFrom-Json
$PackageSource = Get-Content (Join-Path $PSScriptRoot "Package-Windows.ps1") -Raw
$Failures = @()

if ($Manifest.Version -ne "1.0.0") {
    $Failures += "manifest.json is not set to release version 1.0.0."
}
if ($Manifest.UniqueID -ne "Mercury.WarpBookmarks") {
    $Failures += "The stable Mod UniqueID changed unexpectedly."
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
    "docs\WINDOWS_RELEASE_TEST_1.0.0.md"
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
