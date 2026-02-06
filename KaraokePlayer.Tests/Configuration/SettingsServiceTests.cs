using System.IO.Abstractions.TestingHelpers;
using KaraokePlayer.Configuration;
using NUnit.Framework;

namespace KaraokePlayer.Tests.Configuration;

public class SettingsServiceTests
{
  private const string _basePath = @"c:\app";
  private const string _settingsPath = @"c:\app\karaoke.settings.json";
  private const string _defaultLanguage = "en-US";
  private const string _defaultSongsPath = @"c:\app\songs";
  private const string _defaultWindowMode = "BorderlessFullscreen";
  private const int _defaultWidth = 1280;
  private const int _defaultHeight = 720;

  [Test]
  public void Load_FileMissing_ReturnsDefaultSettings()
  {
    // Arrange
    var fileSystemMock = new MockFileSystem();
    var service = new SettingsService(_basePath, fileSystemMock);

    // Act
    var settings = service.Load();

    // Assert
    AssertThatDefaults(settings);
  }

  [Test]
  public void Save_ThenLoad_RoundTripsLanguageCode()
  {
    // Arrange
    const string languageCode = "nl-NL";
    const string songsFolder = @"c:\songs";
    const string windowMode = "Windowed";
    var fileSystemMock = new MockFileSystem();
    var service = new SettingsService(_basePath, fileSystemMock);

    // Act
    service.Save(new AppSettings { LanguageCode = languageCode, SongsFolderPath = songsFolder, WindowMode = windowMode });
    var settings = service.Load();

    // Assert
    Assert.That(settings.LanguageCode, Is.EqualTo(languageCode));
    Assert.That(settings.SongsFolderPath, Is.EqualTo(songsFolder));
    Assert.That(settings.WindowMode, Is.EqualTo(windowMode));
    Assert.That(settings.WindowedWidth, Is.EqualTo(1280));
    Assert.That(settings.WindowedHeight, Is.EqualTo(720));
  }

  [Test]
  public void Load_InvalidJson_ReturnsDefaultSettings()
  {
    // Arrange
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { _settingsPath, new MockFileData("{\"LanguageCode\":") },
    });
    var service = new SettingsService(_basePath, fileSystemMock);

    // Act
    var settings = service.Load();

    // Assert
    AssertThatDefaults(settings);
  }

  [TestCaseSource(nameof(DefaultNormalizationCases))]
  public void Load_NormalizesMissingValues(string json)
  {
    // Arrange
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { _settingsPath, new MockFileData(json) },
    });
    var service = new SettingsService(_basePath, fileSystemMock);

    // Act
    var settings = service.Load();

    // Assert
    AssertThatDefaults(settings);
  }

  [TestCaseSource(nameof(ClampSizeCases))]
  public void Load_WindowedSizeTooSmall_ClampsToMinimum(string json)
  {
    // Arrange
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { _settingsPath, new MockFileData(json) },
    });
    var service = new SettingsService(_basePath, fileSystemMock);

    // Act
    var settings = service.Load();

    // Assert
    Assert.That(settings.WindowedWidth, Is.EqualTo(_defaultWidth));
    Assert.That(settings.WindowedHeight, Is.EqualTo(_defaultHeight));
  }

  private static IEnumerable<TestCaseData> DefaultNormalizationCases()
  {
    yield return new TestCaseData("{\"LanguageCode\":\"\",\"SongsFolderPath\":\"\",\"WindowMode\":\"\"}");
    yield return new TestCaseData("{\"LanguageCode\":null,\"SongsFolderPath\":null,\"WindowMode\":null}");
    yield return new TestCaseData("{\"LanguageCode\":\" \",\"SongsFolderPath\":\" \",\"WindowMode\":\" \"}");
    yield return new TestCaseData("{}");
  }

  private static IEnumerable<TestCaseData> ClampSizeCases()
  {
    yield return new TestCaseData("{\"WindowedWidth\":800,\"WindowedHeight\":600}");
    yield return new TestCaseData("{\"WindowedWidth\":1279,\"WindowedHeight\":719}");
    yield return new TestCaseData("{\"WindowedWidth\":1,\"WindowedHeight\":1}");
  }

  private static void AssertThatDefaults(AppSettings settings)
  {
    Assert.That(settings.LanguageCode, Is.EqualTo(_defaultLanguage));
    Assert.That(settings.SongsFolderPath, Is.EqualTo(_defaultSongsPath));
    Assert.That(settings.WindowMode, Is.EqualTo(_defaultWindowMode));
    Assert.That(settings.WindowedWidth, Is.EqualTo(_defaultWidth));
    Assert.That(settings.WindowedHeight, Is.EqualTo(_defaultHeight));
  }
}
