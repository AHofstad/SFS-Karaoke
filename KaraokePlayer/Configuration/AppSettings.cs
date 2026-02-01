namespace KaraokePlayer.Configuration;

public sealed class AppSettings
{
  public string? LanguageCode { get; set; }
  public string? SongsFolderPath { get; set; }
  public string? WindowMode { get; set; }
  public int? WindowedWidth { get; set; }
  public int? WindowedHeight { get; set; }
}
