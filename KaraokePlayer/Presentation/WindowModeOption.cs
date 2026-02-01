using KaraokePlayer.Configuration;

namespace KaraokePlayer.Presentation;

public sealed class WindowModeOption
{
  public WindowModeOption(WindowModeType mode, string displayName)
  {
    Mode = mode;
    DisplayName = displayName;
  }

  public WindowModeType Mode { get; }
  public string DisplayName { get; }
}
