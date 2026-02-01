using KaraokePlayer.Configuration;

namespace KaraokePlayer.Presentation;

public static class WindowModeHelper
{
  public static WindowModeType NextMode(WindowModeType current)
  {
    return current switch
    {
      WindowModeType.BorderlessFullscreen => WindowModeType.Windowed,
      _ => WindowModeType.BorderlessFullscreen,
    };
  }
}
