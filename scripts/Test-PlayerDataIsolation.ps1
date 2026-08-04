[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$RepositorySource = Get-Content (Join-Path $ProjectRoot "Framework\BookmarkRepository.cs") -Raw
$EntrySource = Get-Content (Join-Path $ProjectRoot "ModEntry.cs") -Raw
$Failures = @()

$OwnerUsesSave = $RepositorySource -match 'uniqueIDForThisGame'
$OwnerUsesPlayer = $RepositorySource -match 'UniqueMultiplayerID'
$OwnerUsesScreen = $RepositorySource -match 'Context\.ScreenId'
$CacheTracksOwner = $RepositorySource -match 'cachedOwnerKey'
$ReadChecksOwner = $RepositorySource -match 'cachedData\s+is\s+not\s+null[\s\S]*cachedOwnerKey[\s\S]*currentOwnerKey'
$WriteChecksOwner = $RepositorySource -match 'Save\(\)[\s\S]*cachedOwnerKey[\s\S]*currentOwnerKey'
$LifecycleResets = $EntrySource -match 'OnSaveLoaded[\s\S]*ResetCache\(\)' -and $EntrySource -match 'OnReturnedToTitle[\s\S]*ResetCache\(\)'

if (-not $OwnerUsesSave -or -not $OwnerUsesPlayer -or -not $OwnerUsesScreen) {
    $Failures += "Bookmark cache ownership doesn't include save, player, and SMAPI screen identity."
}
if (-not $CacheTracksOwner -or -not $ReadChecksOwner) {
    $Failures += "GetData can return cached bookmark data without proving it belongs to the current player/save."
}
if (-not $WriteChecksOwner) {
    $Failures += "Save can write cached bookmark data without rechecking the current player/save owner."
}
if (-not $LifecycleResets) {
    $Failures += "Save-load and return-to-title lifecycle cache resets are missing."
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "Player data isolation guard failed with $($Failures.Count) error(s)."
}

Write-Host "Player data isolation guards passed: $ProjectRoot"
