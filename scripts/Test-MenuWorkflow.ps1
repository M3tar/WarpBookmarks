[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$EntrySource = Get-Content (Join-Path $ProjectRoot "ModEntry.cs") -Raw
$MenuSource = Get-Content (Join-Path $ProjectRoot "UI\WarpBookmarksMenu.cs") -Raw
$CoordinateSource = Get-Content (Join-Path $ProjectRoot "UI\CoordinateWarpDialog.cs") -Raw
$CategorySource = Get-Content (Join-Path $ProjectRoot "Models\DestinationCategory.cs") -Raw
$ConfigSource = Get-Content (Join-Path $ProjectRoot "Config\ModConfig.cs") -Raw
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
if ($MenuSource -notmatch 'PrimaryWarpButton' -or $MenuSource -notmatch 'menu\.warp-to') {
    $Failures += "The selected destination isn't exposed as the footer's primary warp action."
}
if ($MenuSource -match 'primary\s*\?\s*new Color' -or $MenuSource -match 'primary\s*\?\s*new Color\(82,\s*45,\s*20\)') {
    $Failures += "The enabled primary warp button still uses a darker custom fill or text color that can look disabled."
}
if ($EntrySource -match 'ShowShortcutHintIfNeeded|showedShortcutHint' -or $ConfigSource -match 'ShowShortcutHint') {
    $Failures += "The automatic save-load shortcut HUD or its obsolete setting still exists."
}
if ($MenuSource -notmatch 'shortcutText' -or $EntrySource -notmatch 'menu\.shortcuts[\s\S]*OpenMenuKey[\s\S]*CreateBookmarkKey') {
    $Failures += "The in-menu shortcut reminder no longer follows the player's configured keybinds."
}
if ($MenuSource -match 'private\s+Rectangle\s+RestoreButton') {
    $Failures += "Restore-all is still a global footer button instead of a Hidden-category action."
}
if ($MenuSource -notmatch 'selected\.Kind\s*==\s*WarpDestinationKind\.Default\s*&&\s*selected\.IsHidden[\s\S]*menu\.restore-this[\s\S]*menu\.restore-defaults') {
    $Failures += "Hidden defaults don't expose both contextual restore actions."
}
if ($MenuSource -notmatch 'ListScrollbarTrack' -or $MenuSource -notmatch 'GetListScrollbarThumb') {
    $Failures += "The main destination list doesn't expose a visible scrollbar."
}
if ($MenuSource -notmatch 'Keys\.PageDown' -or $MenuSource -notmatch 'Keys\.Home' -or $MenuSource -notmatch 'Keys\.End') {
    $Failures += "Long-list keyboard navigation is incomplete."
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
