using KaraokePlayer.Presentation;
using NUnit.Framework;

namespace KaraokePlayer.Tests.Presentation;

public class GameViewModelTests
{
  [Test]
  public void UpdatePlaybackProgress_WithValidDuration_UpdatesProgressAndTime()
  {
    // arrange
    const double currentMs = 65000;
    const long totalMs = 130000;
    const double expectedProgress = 50;
    const string expectedTime = "01:05";
    var viewModel = new GameViewModel(new MainViewModel());

    // act
    viewModel.UpdatePlaybackProgress(currentMs, totalMs);

    // assert
    Assert.That(viewModel.RemainingProgress, Is.EqualTo(expectedProgress));
    Assert.That(viewModel.ElapsedTime, Is.EqualTo(expectedTime));
  }

  [Test]
  public void UpdatePlaybackProgress_WithZeroDuration_ResetsProgressAndTime()
  {
    // arrange
    const double currentMs = 15000;
    const long totalMs = 0;
    const double expectedProgress = 0;
    const string expectedTime = "00:00";
    var viewModel = new GameViewModel(new MainViewModel());

    // act
    viewModel.UpdatePlaybackProgress(currentMs, totalMs);

    // assert
    Assert.That(viewModel.RemainingProgress, Is.EqualTo(expectedProgress));
    Assert.That(viewModel.ElapsedTime, Is.EqualTo(expectedTime));
  }

  [Test]
  public void UpdatePlaybackProgress_WithDifferentDurations_UsesProvidedDuration()
  {
    // arrange
    const double currentMs = 30000;
    const long shorterDurationMs = 60000;
    const long longerDurationMs = 120000;
    const double expectedShorterProgress = 50;
    const double expectedLongerProgress = 25;
    var viewModel = new GameViewModel(new MainViewModel());

    // act
    viewModel.UpdatePlaybackProgress(currentMs, shorterDurationMs);
    var shorterProgress = viewModel.RemainingProgress;
    viewModel.UpdatePlaybackProgress(currentMs, longerDurationMs);
    var longerProgress = viewModel.RemainingProgress;

    // assert
    Assert.That(shorterProgress, Is.EqualTo(expectedShorterProgress));
    Assert.That(longerProgress, Is.EqualTo(expectedLongerProgress));
  }

  [Test]
  public void PrepareLyrics_WithNotes_SetsFirstNoteStartMs()
  {
    // arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:1000\n: 8 2 0 hi\nE";
    const double expectedStartMs = 2000.0;
    var fileSystemMock = new System.IO.Abstractions.TestingHelpers.MockFileSystem(new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>
    {
      { $@"{songFolder}\song.txt", new System.IO.Abstractions.TestingHelpers.MockFileData(songTxt) },
    });
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var mainViewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    var viewModel = new GameViewModel(mainViewModel);
    var entry = new KaraokeCore.Library.SongEntry(songFolder, $@"{songFolder}\song.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), $@"{songFolder}\song.mp3", null, null, null);

    // act
    viewModel.PrepareLyrics(entry, fileSystemMock);

    // assert
    Assert.That(viewModel.FirstNoteStartMs, Is.EqualTo(expectedStartMs));
  }

  [Test]
  public void UpdatePlaybackProgress_FirstNoteBeyondThreshold_ShowsSkipPrompt()
  {
    // arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:1000\n: 32 2 0 hi\nE";
    var fileSystemMock = new System.IO.Abstractions.TestingHelpers.MockFileSystem(new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>
    {
      { $@"{songFolder}\song.txt", new System.IO.Abstractions.TestingHelpers.MockFileData(songTxt) },
    });
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var mainViewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    var viewModel = new GameViewModel(mainViewModel);
    var entry = new KaraokeCore.Library.SongEntry(songFolder, $@"{songFolder}\song.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), $@"{songFolder}\song.mp3", null, null, null);
    viewModel.PrepareLyrics(entry, fileSystemMock);

    // act
    viewModel.UpdatePlaybackProgress(0, 60000);

    // assert
    Assert.That(viewModel.IsSkipPromptVisible, Is.True);
  }

  [Test]
  public void UpdatePlaybackProgress_FirstNoteClose_HidesSkipPrompt()
  {
    // arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:0\n: 4 2 0 hi\nE";
    var fileSystemMock = new System.IO.Abstractions.TestingHelpers.MockFileSystem(new Dictionary<string, System.IO.Abstractions.TestingHelpers.MockFileData>
    {
      { $@"{songFolder}\song.txt", new System.IO.Abstractions.TestingHelpers.MockFileData(songTxt) },
    });
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var mainViewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    var viewModel = new GameViewModel(mainViewModel);
    var entry = new KaraokeCore.Library.SongEntry(songFolder, $@"{songFolder}\song.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), $@"{songFolder}\song.mp3", null, null, null);
    viewModel.PrepareLyrics(entry, fileSystemMock);

    // act
    viewModel.UpdatePlaybackProgress(0, 60000);

    // assert
    Assert.That(viewModel.IsSkipPromptVisible, Is.False);
  }
}
