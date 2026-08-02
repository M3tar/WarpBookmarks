[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Failures = @()

Get-ChildItem $ProjectRoot -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
    ForEach-Object {
        $Source = Get-Content $_.FullName -Raw
        if ($Source -match '\bContext\.' -and $Source -notmatch 'using\s+StardewModdingAPI\s*;') {
            $Failures += "$($_.FullName): uses SMAPI Context without 'using StardewModdingAPI;'"
        }
    }

if ($Failures.Count -gt 0) {
    $Failures | ForEach-Object { Write-Error $_ }
    throw "C# namespace guard failed with $($Failures.Count) error(s)."
}

Write-Host "C# namespace guards passed: $ProjectRoot"
