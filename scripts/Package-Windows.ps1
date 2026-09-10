[CmdletBinding()]
param(
    [string] $GamePath
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$BuildScript = Join-Path $PSScriptRoot "Build-Windows.ps1"
$ManifestPath = Join-Path $ProjectRoot "manifest.json"
$Manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json

& $BuildScript -GamePath $GamePath -Configuration Release

$BuildOutput = Join-Path $ProjectRoot "bin\Release\net6.0"
$AssemblyPath = Join-Path $BuildOutput "WarpBookmarks.dll"
if (-not (Test-Path $AssemblyPath)) {
    throw "Release assembly was not found at '$AssemblyPath'."
}

$DistPath = Join-Path $ProjectRoot "dist"
$StagePath = Join-Path $DistPath "release-stage"
$ModPath = Join-Path $StagePath "WarpBookmarks"
$ZipPath = Join-Path $DistPath "WarpBookmarks-$($Manifest.Version).zip"

if (Test-Path $StagePath) {
    Remove-Item $StagePath -Recurse -Force
}
New-Item -ItemType Directory -Path $ModPath -Force | Out-Null

Copy-Item $AssemblyPath $ModPath -Force
Copy-Item $ManifestPath $ModPath -Force
Copy-Item (Join-Path $ProjectRoot "i18n") $ModPath -Recurse -Force

# Windows PowerShell 5.1 doesn't always load the assembly containing ZipArchive
# when only the FileSystem helper assembly is requested.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}
$ArchiveStream = [System.IO.File]::Open($ZipPath, [System.IO.FileMode]::CreateNew)
$Writer = [System.IO.Compression.ZipArchive]::new(
    $ArchiveStream,
    [System.IO.Compression.ZipArchiveMode]::Create
)
try {
    $FilesToPackage = Get-ChildItem $ModPath -File -Recurse
    foreach ($File in $FilesToPackage) {
        $RelativePath = $File.FullName.Substring($StagePath.Length + 1).Replace("\", "/")
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $Writer,
            $File.FullName,
            $RelativePath,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
}
finally {
    $Writer.Dispose()
    $ArchiveStream.Dispose()
}

$Archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
try {
    $Entries = @($Archive.Entries | ForEach-Object { $_.FullName.Replace("\", "/") })
}
finally {
    $Archive.Dispose()
}

$RequiredEntries = @(
    "WarpBookmarks/WarpBookmarks.dll",
    "WarpBookmarks/manifest.json",
    "WarpBookmarks/i18n/default.json",
    "WarpBookmarks/i18n/zh.json"
)
foreach ($RequiredEntry in $RequiredEntries) {
    if ($Entries -notcontains $RequiredEntry) {
        throw "Release package is missing '$RequiredEntry'."
    }
}

$UnexpectedEntries = @($Entries | Where-Object {
    $_ -match '\.(cs|csproj|pdb|zip)$' -or $_ -match '(^|/)(bin|obj|dist)/'
})
if ($UnexpectedEntries.Count -gt 0) {
    throw "Release package contains development files: $($UnexpectedEntries -join ', ')"
}

Remove-Item $StagePath -Recurse -Force
$Hash = Get-FileHash $ZipPath -Algorithm SHA256

Write-Host "Nexus-ready package: $ZipPath"
Write-Host "SHA-256: $($Hash.Hash.ToLowerInvariant())"
Write-Host "Run the checks in docs\WINDOWS_RELEASE_TEST_$($Manifest.Version).md before uploading."
