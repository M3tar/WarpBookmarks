[CmdletBinding()]
param(
    [string] $ScriptPath = (Join-Path $PSScriptRoot "Build-Windows.ps1")
)

$ErrorActionPreference = "Stop"
$ResolvedScriptPath = (Resolve-Path $ScriptPath).Path
$Tokens = $null
$ParseErrors = $null

[System.Management.Automation.Language.Parser]::ParseFile(
    $ResolvedScriptPath,
    [ref] $Tokens,
    [ref] $ParseErrors
) | Out-Null

if ($ParseErrors.Count -gt 0) {
    foreach ($ParseError in $ParseErrors) {
        Write-Error "$($ParseError.Extent.StartLineNumber):$($ParseError.Extent.StartColumnNumber) $($ParseError.Message)"
    }
    throw "PowerShell syntax validation failed for '$ResolvedScriptPath'."
}

Write-Host "PowerShell syntax is valid: $ResolvedScriptPath"
