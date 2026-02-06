# Builds a self-contained portable win-x64 release.
$ErrorActionPreference = 'Stop'

$projectPath = Join-Path $PSScriptRoot '..\KaraokePlayer\KaraokePlayer.csproj'
$publishDir = Join-Path $PSScriptRoot '..\KaraokePlayer\bin\Release\net10.0-windows\win-x64\publish'
$libVlcTarget = Join-Path $publishDir 'libvlc'
$libVlcTargetWin64 = Join-Path $libVlcTarget 'win-x64'
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\\packages' }
$libVlcPackageRoots = Get-ChildItem (Join-Path $nugetRoot 'videolan.libvlc.windows') -Directory -ErrorAction SilentlyContinue |
  Sort-Object Name -Descending

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

# Ensure win-x64 plugins are present.
$pluginsPath = Join-Path $libVlcTargetWin64 'plugins'
if (-not (Test-Path $pluginsPath)) {
  $candidateRoots = @()
  foreach ($pkgRoot in $libVlcPackageRoots) {
    $candidateRoots += Join-Path $pkgRoot.FullName 'runtimes\\win-x64\\native'
    $candidateRoots += Join-Path $pkgRoot.FullName 'build\\x64'
  }

  $libVlcSourceRoot = $candidateRoots |
    Where-Object { Test-Path (Join-Path $_ 'plugins') } |
    Select-Object -First 1

  if ($libVlcSourceRoot) {
    New-Item -ItemType Directory -Path $libVlcTargetWin64 -Force | Out-Null
    Copy-Item (Join-Path $libVlcSourceRoot '*') $libVlcTargetWin64 -Recurse -Force
    Write-Host "Copied missing libvlc win-x64 runtime/plugins from NuGet cache." -ForegroundColor Cyan
  } else {
    throw "libvlc win-x64 plugins are missing in publish output and no valid NuGet source was found."
  }
}

# Remove x86 runtime artifacts to keep portable output minimal.
$x86RuntimeDir = Join-Path $libVlcTarget 'win-x86'
if (Test-Path $x86RuntimeDir) {
  Remove-Item $x86RuntimeDir -Recurse -Force
  Write-Host "Removed libvlc\\win-x86 from publish output." -ForegroundColor Cyan
}

# Final validation.
if (-not (Test-Path (Join-Path $libVlcTargetWin64 'plugins'))) {
  throw "Portable output is missing libvlc win-x64 plugins."
}

# Remove debug symbols if present.
Get-ChildItem $publishDir -Filter '*.pdb' -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "Done. Portable output: $publishDir" -ForegroundColor Green
