using System.ComponentModel;
using System.Runtime.CompilerServices;
using KaraokePlayer.Configuration;

namespace KaraokePlayer.Presentation;

public sealed class OptionsViewModel : INotifyPropertyChanged
{
  private readonly SettingsService _settingsService;
  private string _songsFolderPath = string.Empty;
  private WindowModeOption _windowMode;
  private bool _isLoading;
  private bool _suppressSave;

  public OptionsViewModel(SettingsService settingsService)
  {
    _settingsService = settingsService;
    WindowModes = new List<WindowModeOption>
    {
      new WindowModeOption(Configuration.WindowModeType.BorderlessFullscreen, KaraokePlayer.Resources.Strings.WindowModeBorderless),
      new WindowModeOption(Configuration.WindowModeType.Windowed, KaraokePlayer.Resources.Strings.WindowModeWindowed),
    };
    _windowMode = WindowModes[0];
    Load();
  }

  public string SongsFolderPath
  {
    get => _songsFolderPath;
    set
    {
      if (!SetField(ref _songsFolderPath, value))
      {
        return;
      }

      if (_isLoading || _suppressSave)
      {
        return;
      }

      Save();
      SongsFolderChanged?.Invoke(this, _songsFolderPath);
    }
  }

  public IReadOnlyList<WindowModeOption> WindowModes { get; }

  public WindowModeOption WindowMode
  {
    get => _windowMode;
    set
    {
      if (!SetField(ref _windowMode, value))
      {
        return;
      }

      if (_isLoading)
      {
        return;
      }

      Save();
      WindowModeChanged?.Invoke(this, _windowMode.Mode);
    }
  }

  public event PropertyChangedEventHandler? PropertyChanged;
  public event EventHandler<Configuration.WindowModeType>? WindowModeChanged;
  public event EventHandler<string>? SongsFolderChanged;

  public void Load()
  {
    _isLoading = true;
    var settings = _settingsService.Load();
    SongsFolderPath = settings.SongsFolderPath ?? string.Empty;
    WindowMode = ResolveWindowMode(settings.WindowMode);
    _isLoading = false;
  }

  public void Save()
  {
    if (_suppressSave)
    {
      return;
    }

    var settings = _settingsService.Load();
    settings.SongsFolderPath = SongsFolderPath;
    settings.WindowMode = WindowMode.Mode.ToString();
    _settingsService.Save(settings);
  }

  public void UpdateSongsFolder(string path)
  {
    _suppressSave = true;
    SongsFolderPath = path;
    _suppressSave = false;
    Save();
    SongsFolderChanged?.Invoke(this, path);
  }

  private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
  {
    if (EqualityComparer<T>.Default.Equals(field, value))
    {
      return false;
    }

    field = value;
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    return true;
  }

  private WindowModeOption ResolveWindowMode(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return WindowModes[0];
    }

    if (Enum.TryParse<Configuration.WindowModeType>(value, true, out var parsed))
    {
      var match = WindowModes.FirstOrDefault(option => option.Mode == parsed);
      if (match is not null)
      {
        return match;
      }
    }

    return WindowModes[0];
  }
}
