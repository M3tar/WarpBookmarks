[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$WarpSource = Get-Content (Join-Path $ProjectRoot "Framework\WarpService.cs") -Raw
$DefaultI18n = Get-Content (Join-Path $ProjectRoot "i18n\default.json") -Raw | ConvertFrom-Json
$ChineseI18n = Get-Content (Join-Path $ProjectRoot "i18n\zh.json") -Raw | ConvertFrom-Json
$Failures = @()

foreach ($I18n in @($DefaultI18n, $ChineseI18n)) {
    $NormalMessage = $I18n.'hud.warp-success'
    $AdjustedMessage = $I18n.'hud.warp-success-adjusted'
    if ($NormalMessage -match '\{\{x\}\}|\{\{y\}\}') {
        $Failures += "Normal warp success text still appends coordinates to a destination name that may already contain them."
    }
    if (-not $AdjustedMessage -or $AdjustedMessage -notmatch '\{\{x\}\}' -or $AdjustedMessage -notmatch '\{\{y\}\}') {
        $Failures += "The adjusted-landing success text doesn't report the corrected safe tile."
    }
}

$ComparesRequestedAndSafeTiles = $WarpSource -match 'landingAdjusted[\s\S]*safeTile\.X[\s\S]*destination\.Location\.TileX'
$ChoosesAdjustedMessage = $WarpSource -match 'landingAdjusted[\s\S]*hud\.warp-success-adjusted'
if (-not $ComparesRequestedAndSafeTiles -or -not $ChoosesAdjustedMessage) {
    $Failures += "WarpService doesn't select the adjusted-landing message by comparing the safe tile with the requested tile."
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "Warp HUD formatting guard failed with $($Failures.Count) error(s)."
}

Write-Host "Warp HUD formatting guards passed: $ProjectRoot"
