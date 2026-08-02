[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$CoordinatePath = Join-Path $ProjectRoot "UI\CoordinateWarpDialog.cs"
$RenamePath = Join-Path $ProjectRoot "UI\BookmarkRenameDialog.cs"
$EntryPath = Join-Path $ProjectRoot "ModEntry.cs"

$Failures = @()
$CoordinateSource = Get-Content $CoordinatePath -Raw
if ($CoordinateSource -notmatch 'ActiveTextBox\s+is\s+not\s+null') {
    $Failures += "CoordinateWarpDialog doesn't reserve focused-textbox keys for the keyboard dispatcher."
}
if ($CoordinateSource -match 'Keys\.Tab\s*\|\|' -or $CoordinateSource -match 'Keys\.Right\s*&&\s*this\.xBox\.Selected') {
    $Failures += "CoordinateWarpDialog still changes X/Y focus from Tab or arrow keys."
}
if (-not (Test-Path $RenamePath)) {
    $Failures += "The custom BookmarkRenameDialog is missing."
}
elseif ((Get-Content $RenamePath -Raw) -notmatch 'inputBox\.Selected') {
    $Failures += "BookmarkRenameDialog doesn't reserve focused-textbox keys for the keyboard dispatcher."
}
if ((Get-Content $EntryPath -Raw) -match 'new\s+NamingMenu\s*\(') {
    $Failures += "ModEntry still uses the game's NamingMenu instead of the controlled bookmark editor."
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "Text input routing guard failed with $($Failures.Count) error(s)."
}

Write-Host "Text input routing guards passed: $ProjectRoot"
