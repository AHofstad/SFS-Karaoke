using System.Text.Json;
using System.IO.Abstractions;

namespace KaraokePlayer.Configuration;

public sealed class SettingsService
{
  private const string DefaultFileName = "karaoke.settings.json";
  private const string DefaultLanguageCode = "en-US";
  private const string DefaultSongsFolderName = "songs";
  private const string DefaultWindowMode = "BorderlessFullscreen";
  private const int DefaultWindowedWidth = 1280;
  private const int DefaultWindowedHeight = 720;

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
    return _fileSystem.Path.Combine(_basePath, DefaultFileName);
  }

  private AppSettings CreateDefaultSettings()
  {
    return new AppSettings
    {
      LanguageCode = DefaultLanguageCode,
      SongsFolderPath = _fileSystem.Path.Combine(_basePath, DefaultSongsFolderName),
      WindowMode = DefaultWindowMode,
      WindowedWidth = DefaultWindowedWidth,
      WindowedHeight = DefaultWindowedHeight,
    };
  }

  private AppSettings Normalize(AppSettings settings)
  {
    if (string.IsNullOrWhiteSpace(settings.LanguageCode))
    {
      settings.LanguageCode = DefaultLanguageCode;
    }

    if (string.IsNullOrWhiteSpace(settings.SongsFolderPath))
    {
      settings.SongsFolderPath = _fileSystem.Path.Combine(_basePath, DefaultSongsFolderName);
    }

    if (string.IsNullOrWhiteSpace(settings.WindowMode))
    {
      settings.WindowMode = DefaultWindowMode;
    }

    if (!settings.WindowedWidth.HasValue || settings.WindowedWidth.Value < DefaultWindowedWidth)
    {
      settings.WindowedWidth = DefaultWindowedWidth;
    }

    if (!settings.WindowedHeight.HasValue || settings.WindowedHeight.Value < DefaultWindowedHeight)
    {
      settings.WindowedHeight = DefaultWindowedHeight;
    }

    return settings;
  }
}
