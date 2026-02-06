# Builds a self-contained portable win-x64 release.
$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot '..\KaraokePlayer\KaraokePlayer.csproj'
$publishDir = Join-Path $PSScriptRoot '..\KaraokePlayer\bin\Release\net10.0-windows\win-x64\publish'
$libVlcTarget = Join-Path $publishDir 'libvlc'
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\\packages' }
$libVlcPackageRoots = Get-ChildItem (Join-Path $nugetRoot 'videolan.libvlc.windows') -Directory -ErrorAction SilentlyContinue |
  Sort-Object Name -Descending |
  ForEach-Object { Join-Path $_.FullName 'runtimes\\win-x64\\native' }
$libVlcPackageRoot = $libVlcPackageRoots | Where-Object { Test-Path $_ } | Select-Object -First 1

Write-Host "Publishing to $publishDir" -ForegroundColor Cyan

dotnet publish $projectPath `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false `
  -p:DebugSymbols=false `
  -p:DebugType=None

if ($libVlcPackageRoot -and (Test-Path $libVlcPackageRoot)) {
  if (Test-Path $libVlcTarget) {
    Remove-Item $libVlcTarget -Recurse -Force
  }
  New-Item -ItemType Directory -Path $libVlcTarget -Force | Out-Null
  Copy-Item (Join-Path $libVlcPackageRoot '*') $libVlcTarget -Recurse -Force
  Write-Host "Copied libvlc x64 runtime from NuGet cache to publish output." -ForegroundColor Cyan
} else {
  Write-Host "LibVLC x64 runtime not found in NuGet cache. Keeping published libvlc output." -ForegroundColor Yellow
}

# Remove x86 runtime artifacts to keep portable output minimal.
$x86RuntimeDir = Join-Path $publishDir 'libvlc\\win-x86'
if (Test-Path $x86RuntimeDir) {
  Remove-Item $x86RuntimeDir -Recurse -Force
  Write-Host "Removed libvlc\\win-x86 from publish output." -ForegroundColor Cyan
}

# Remove debug symbols if present.
Get-ChildItem $publishDir -Filter '*.pdb' -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "Done. Portable output: $publishDir" -ForegroundColor Green
