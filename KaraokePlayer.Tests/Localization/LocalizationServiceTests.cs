using KaraokePlayer.Configuration;
using KaraokePlayer.Localization;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;

namespace KaraokePlayer.Tests.Localization;

public class LocalizationServiceTests
{
  private const string BasePath = @"c:\app";
  private const string SettingsPath = @"c:\app\karaoke.settings.json";

  [Test]
  public void Constructor_LoadsLanguageFromSettings()
  {
    // Arrange
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { SettingsPath, new MockFileData("{\"LanguageCode\":\"nl-NL\"}") },
    });
    var settingsService = new SettingsService(BasePath, fileSystemMock);

    // Act
    var service = new LocalizationService(settingsService);

    // Assert
    Assert.That(service.CurrentLanguage.Code, Is.EqualTo("nl-NL"));
  }

  [Test]
  public void CurrentLanguage_Change_SavesSettings()
  {
    // Arrange
    var fileSystemMock = new MockFileSystem();
    var settingsService = new SettingsService(BasePath, fileSystemMock);
    var service = new LocalizationService(settingsService);
    var dutch = service.Languages.First(language => language.Code == "nl-NL");

    // Act
    service.CurrentLanguage = dutch;

    // Assert
    var settings = settingsService.Load();
    Assert.That(settings.LanguageCode, Is.EqualTo("nl-NL"));
  }

  [Test]
  public void CurrentLanguage_Change_RaisesIndexerNotification()
  {
    // Arrange
    var fileSystemMock = new MockFileSystem();
    var settingsService = new SettingsService(BasePath, fileSystemMock);
    var service = new LocalizationService(settingsService);
    var dutch = service.Languages.First(language => language.Code == "nl-NL");
    string? lastProperty = null;
    service.PropertyChanged += (_, args) => lastProperty = args.PropertyName;

    // Act
    service.CurrentLanguage = dutch;

    // Assert
    Assert.That(lastProperty, Is.EqualTo("Item[]"));
  }

  [TestCase("en-US")]
  [TestCase("nl-NL")]
  [TestCase("EN-us")]
  public void Constructor_LoadsLanguageFromSettings_MultipleCases(string code)
  {
    // Arrange
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { SettingsPath, new MockFileData($"{{\"LanguageCode\":\"{code}\"}}") },
    });
    var settingsService = new SettingsService(BasePath, fileSystemMock);

    // Act
    var service = new LocalizationService(settingsService);

    // Assert
    Assert.That(service.CurrentLanguage.Code, Is.EqualTo(code.ToLowerInvariant() == "en-us" ? "en-US" : "nl-NL"));
  }
}
