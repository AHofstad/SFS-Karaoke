# SFS Karaoke

SFS Karaoke is a Windows karaoke player for UltraStar-style songs (`.txt` metadata/lyrics + media files).

## What It Does

- Scans a songs folder and imports playable entries.
- Plays audio/video with lyric timing from UltraStar metadata.
- Provides queue-based song selection with preview playback.
- Supports keyboard shortcuts during playback (pause, seek, skip behavior).

## Nightly Build

Portable self-contained nightly builds are published from GitHub Actions artifacts.

- Download latest nightly:  
  [https://nightly.link/AHofstad/SFS-Karaoke/workflows/nightly.yml/master/KaraokePlayerPortable.zip](https://nightly.link/AHofstad/SFS-Karaoke/workflows/nightly.yml/master/KaraokePlayerPortable.zip)

If the link does not show a file yet, wait for the first successful run of the `nightly` workflow.

## Local Build

- Build app:
  - `dotnet build .\KaraokePlayer\KaraokePlayer.csproj -c Release`
- Build portable zip (self-contained runtime included):
  - `.\scripts\build-portable-zip.ps1`
