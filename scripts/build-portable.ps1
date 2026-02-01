# Builds a self-contained portable win-x64 release.
$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot '..\KaraokePlayer\KaraokePlayer.csproj'
$publishDir = Join-Path $PSScriptRoot '..\KaraokePlayer\bin\Release\net10.0-windows\win-x64\publish'
$libVlcTarget = Join-Path $publishDir 'libvlc'
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\\packages' }
$libVlcPackageRoot = Join-Path $nugetRoot 'videolan.libvlc.windows\\3.0.20\\runtimes\\win-x64\\native'

Write-Host "Publishing to $publishDir" -ForegroundColor Cyan

dotnet publish $projectPath `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false

if (Test-Path $libVlcPackageRoot) {
  if (Test-Path $libVlcTarget) {
    Remove-Item $libVlcTarget -Recurse -Force
  }
  New-Item -ItemType Directory -Path $libVlcTarget -Force | Out-Null
  Copy-Item (Join-Path $libVlcPackageRoot '*') $libVlcTarget -Recurse -Force
  Write-Host "Copied libvlc from NuGet cache to publish output." -ForegroundColor Cyan
} else {
  Write-Host "LibVLC package not found at $libVlcPackageRoot" -ForegroundColor Yellow
}

Write-Host "Done. Portable output: $publishDir" -ForegroundColor Green
