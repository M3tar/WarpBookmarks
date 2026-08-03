[CmdletBinding()]
param(
    [string[]] $ScriptPath
)

$ErrorActionPreference = "Stop"
$PathsToCheck = if ($ScriptPath.Count -gt 0) {
    $ScriptPath | ForEach-Object { (Resolve-Path $_).Path }
}
else {
    Get-ChildItem $PSScriptRoot -Filter "*.ps1" -File | Select-Object -ExpandProperty FullName
}
$Failures = @()

foreach ($ResolvedScriptPath in $PathsToCheck) {
    $Tokens = $null
    $ParseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile(
        $ResolvedScriptPath,
        [ref] $Tokens,
        [ref] $ParseErrors
    ) | Out-Null

    foreach ($ParseError in $ParseErrors) {
        $Failures += "$ResolvedScriptPath`:$($ParseError.Extent.StartLineNumber):$($ParseError.Extent.StartColumnNumber) $($ParseError.Message)"
    }
}

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "PowerShell syntax validation failed with $($Failures.Count) error(s)."
}

Write-Host "PowerShell syntax is valid for $($PathsToCheck.Count) script(s): $PSScriptRoot"
