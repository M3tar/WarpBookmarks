[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$EntrySource = Get-Content (Join-Path $ProjectRoot "ModEntry.cs") -Raw
$MenuSource = Get-Content (Join-Path $ProjectRoot "UI\WarpBookmarksMenu.cs") -Raw
$CoordinateSource = Get-Content (Join-Path $ProjectRoot "UI\CoordinateWarpDialog.cs") -Raw
$CategorySource = Get-Content (Join-Path $ProjectRoot "Models\DestinationCategory.cs") -Raw
$Failures = @()

if ($CategorySource -notmatch 'Common\s*,\s*All\s*,\s*Bookmarks\s*,\s*Defaults\s*,\s*Hidden') {
    $Failures += "The five menu categories aren't in the expected UI order."
}
if ($MenuSource -notmatch 'WarpDestinationKind\.Home\s+or\s+WarpDestinationKind\.Previous' -or $MenuSource -notmatch 'destination\.IsFavorite') {
    $Failures += "The Common view doesn't include home/previous and favorites."
}
if ($EntrySource -notmatch 'lastMenuCategory\s*=\s*DestinationCategory\.Common') {
    $Failures += "The menu session doesn't start from Common."
}
if ($MenuSource -notmatch 'categoryChanged\(this\.selectedCategory\)') {
    $Failures += "Category changes aren't reported for session memory."
}
if ($CoordinateSource -notmatch 'VisibleDropdownRows\s*=\s*7') {
    $Failures += "The map dropdown isn't expanded to seven visible rows."
}
if ($CoordinateSource -notmatch 'ScrollbarTrack' -or $CoordinateSource -notmatch 'GetScrollbarThumb') {
    $Failures += "The long map dropdown doesn't expose a scrollbar."
}
if ($EntrySource -notmatch 'OpenCreateCurrentBookmarkDialog' -or $EntrySource -notmatch 'OpenCreateCoordinateBookmarkDialog') {
    $Failures += "Manual and coordinate bookmark saves don't both use pre-save naming."
}
if ($EntrySource -notmatch 'CreateBookmarkKey\.JustPressed\(\)[\s\S]*CreateBookmarkAtCurrentLocation') {
    $Failures += "The quick-record shortcut no longer uses the immediate save path."
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "Menu workflow guard failed with $($Failures.Count) error(s)."
}

Write-Host "Menu workflow guards passed: $ProjectRoot"
