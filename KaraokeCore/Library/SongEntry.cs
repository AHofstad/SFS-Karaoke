using KaraokeCore.Models;

namespace KaraokeCore.Library;

public sealed class SongEntry
{
  public SongEntry(
    string folderPath,
    string? txtPath,
    SongMetadata? metadata,
    string? audioPath,
    string? videoPath,
    string? coverPath,
    string? backgroundPath
  )
  {
    FolderPath = folderPath;
    TxtPath = txtPath;
    Metadata = metadata;
    AudioPath = audioPath;
    VideoPath = videoPath;
    CoverPath = coverPath;
    BackgroundPath = backgroundPath;
  }

  public string FolderPath { get; }
  public string? TxtPath { get; }
  public SongMetadata? Metadata { get; }
  public string? AudioPath { get; }
  public string? VideoPath { get; }
  public string? CoverPath { get; }
  public string? BackgroundPath { get; }
}
