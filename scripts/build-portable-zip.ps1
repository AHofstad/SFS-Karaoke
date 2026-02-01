# Builds and zips a self-contained portable win-x64 release.
$ErrorActionPreference = 'Stop'

$scriptRoot = $PSScriptRoot
$projectRoot = Resolve-Path (Join-Path $scriptRoot '..')
$outputRoot = Join-Path $projectRoot 'dist'
$publishDir = Join-Path $projectRoot 'KaraokePlayer\bin\Release\net10.0-windows\win-x64\publish'
$zipPath = Join-Path $outputRoot 'KaraokePlayerPortable.zip'

& (Join-Path $scriptRoot 'build-portable.ps1')

if (-not (Test-Path $outputRoot)) {
  New-Item -ItemType Directory -Path $outputRoot | Out-Null
}

if (Test-Path $zipPath) {
  Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath

Write-Host "Created $zipPath" -ForegroundColor Green
