using KaraokePlayer.Configuration;
using KaraokePlayer.Presentation;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;

namespace KaraokePlayer.Tests.Presentation;

public class OptionsViewModelTests
{
  [Test]
  public void WindowMode_Change_SavesImmediately()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new SettingsService(basePath, fileSystemMock);
    var viewModel = new OptionsViewModel(settingsService);
    var windowed = viewModel.WindowModes.First(option => option.Mode == WindowModeType.Windowed);

    // Act
    viewModel.WindowMode = windowed;

    // Assert
    var settings = settingsService.Load();
    Assert.That(settings.WindowMode, Is.EqualTo("Windowed"));
  }

  [Test]
  public void SongsFolder_Change_SavesImmediately()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string newPath = @"c:\songs";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new SettingsService(basePath, fileSystemMock);
    var viewModel = new OptionsViewModel(settingsService);

    // Act
    viewModel.SongsFolderPath = newPath;

    // Assert
    var settings = settingsService.Load();
    Assert.That(settings.SongsFolderPath, Is.EqualTo(newPath));
  }

  [Test]
  public void UpdateSongsFolder_ChangesAndRaisesEvent()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string newPath = @"c:\songs";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new SettingsService(basePath, fileSystemMock);
    var viewModel = new OptionsViewModel(settingsService);
    string? raisedPath = null;
    viewModel.SongsFolderChanged += (_, path) => raisedPath = path;

    // Act
    viewModel.UpdateSongsFolder(newPath);

    // Assert
    Assert.That(viewModel.SongsFolderPath, Is.EqualTo(newPath));
    Assert.That(raisedPath, Is.EqualTo(newPath));
  }
}
