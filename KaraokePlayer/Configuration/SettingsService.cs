using System.IO.Abstractions;
using System.Text.Json;

namespace KaraokePlayer.Configuration;

public sealed class SettingsService
{
  private const string _defaultFileName = "karaoke.settings.json";
  private const string _defaultLanguageCode = "en-US";
  private const string _defaultSongsFolderName = "songs";
  private const string _defaultWindowMode = "BorderlessFullscreen";
  private const int _defaultWindowedWidth = 1280;
  private const int _defaultWindowedHeight = 720;

  public static SettingsService Instance { get; } = new(AppDomain.CurrentDomain.BaseDirectory, new FileSystem());

  private readonly string _basePath;
  private readonly IFileSystem _fileSystem;

  public SettingsService(string basePath, IFileSystem fileSystem)
  {
    _basePath = basePath;
    _fileSystem = fileSystem;
  }

  public AppSettings Load()
  {
    var path = GetSettingsPath();
    if (!_fileSystem.File.Exists(path))
    {
      return CreateDefaultSettings();
    }

    try
    {
      var json = _fileSystem.File.ReadAllText(path);
      var settings = JsonSerializer.Deserialize<AppSettings>(json);
      if (settings is null)
      {
        return CreateDefaultSettings();
      }

      return Normalize(settings);
    }
    catch (JsonException)
    {
      return CreateDefaultSettings();
    }
  }

  public void Save(AppSettings settings)
  {
    var path = GetSettingsPath();
    var directory = _fileSystem.Path.GetDirectoryName(path);
    if (!string.IsNullOrWhiteSpace(directory) && !_fileSystem.Directory.Exists(directory))
    {
      _fileSystem.Directory.CreateDirectory(directory);
    }
    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
    _fileSystem.File.WriteAllText(path, json);
  }

  private string GetSettingsPath()
  {
    return _fileSystem.Path.Combine(_basePath, _defaultFileName);
  }

  private AppSettings CreateDefaultSettings()
  {
    return new AppSettings
    {
      LanguageCode = _defaultLanguageCode,
      SongsFolderPath = _fileSystem.Path.Combine(_basePath, _defaultSongsFolderName),
      WindowMode = _defaultWindowMode,
      WindowedWidth = _defaultWindowedWidth,
      WindowedHeight = _defaultWindowedHeight,
    };
  }

  private AppSettings Normalize(AppSettings settings)
  {
    if (string.IsNullOrWhiteSpace(settings.LanguageCode))
    {
      settings.LanguageCode = _defaultLanguageCode;
    }

    if (string.IsNullOrWhiteSpace(settings.SongsFolderPath))
    {
      settings.SongsFolderPath = _fileSystem.Path.Combine(_basePath, _defaultSongsFolderName);
    }

    if (string.IsNullOrWhiteSpace(settings.WindowMode))
    {
      settings.WindowMode = _defaultWindowMode;
    }

    if (!settings.WindowedWidth.HasValue || settings.WindowedWidth.Value < _defaultWindowedWidth)
    {
      settings.WindowedWidth = _defaultWindowedWidth;
    }

    if (!settings.WindowedHeight.HasValue || settings.WindowedHeight.Value < _defaultWindowedHeight)
    {
      settings.WindowedHeight = _defaultWindowedHeight;
    }

    return settings;
  }
}
