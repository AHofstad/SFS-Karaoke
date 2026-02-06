using KaraokePlayer.Presentation;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;

namespace KaraokePlayer.Tests.Presentation;

public class MainViewModelTests
{
  [Test]
  public void LoadSongs_SongsFolderExists_PopulatesSongsAndStatus()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songsPath = @"c:\app\songs";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE";
    var expectedStatus = KaraokePlayer.Resources.Strings.StatusLoaded;
    const int expectedCount = 1;
    const double expectedProgress = 100;

    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });

    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);

    // Act
    viewModel.LoadSongs();

    // Assert
    Assert.That(viewModel.LibraryPath, Is.EqualTo(songsPath));
    Assert.That(viewModel.Status, Is.EqualTo(expectedStatus));
    Assert.That(viewModel.Songs.Count, Is.EqualTo(expectedCount));
    Assert.That(viewModel.LoadingProgress, Is.EqualTo(expectedProgress));
  }

  [Test]
  public void LoadSongs_AfterCompletion_IsLoadingFalse()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE";
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });
    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);

    // Act
    viewModel.LoadSongs();

    // Assert
    Assert.That(viewModel.IsLoading, Is.False);
  }

  [Test]
  public void LoadSongs_SongsFolderMissing_SetsMissingStatusAndEmptyList()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songsPath = @"c:\app\songs";
    var expectedStatus = KaraokePlayer.Resources.Strings.StatusMissing;
    const int expectedCount = 0;
    const double expectedProgress = 100;
    var fileSystemMock = new MockFileSystem();
    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);

    // Act
    viewModel.LoadSongs();

    // Assert
    Assert.That(viewModel.LibraryPath, Is.EqualTo(songsPath));
    Assert.That(viewModel.Status, Is.EqualTo(expectedStatus));
    Assert.That(viewModel.Songs.Count, Is.EqualTo(expectedCount));
    Assert.That(viewModel.LoadingProgress, Is.EqualTo(expectedProgress));
  }

  [Test]
  public void LoadSongs_EmptyLibrary_SetsMissingStatus()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songsPath = @"c:\app\songs";
    var expectedStatus = KaraokePlayer.Resources.Strings.StatusMissing;
    const int expectedCount = 0;
    const double expectedProgress = 100;
    var fileSystemMock = new MockFileSystem();
    fileSystemMock.AddDirectory(songsPath);
    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));

    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);

    // Act
    viewModel.LoadSongs();

    // Assert
    Assert.That(viewModel.LibraryPath, Is.EqualTo(songsPath));
    Assert.That(viewModel.Status, Is.EqualTo(expectedStatus));
    Assert.That(viewModel.Songs.Count, Is.EqualTo(expectedCount));
    Assert.That(viewModel.LoadingProgress, Is.EqualTo(expectedProgress));
  }

  [Test]
  public void SelectedSong_WithNotes_ComputesFirstNoteStartMs()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songsPath = @"c:\app\songs";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:1000\n: 8 2 0 hi\nE";
    const double expectedFirstNoteMs = 2000.0;
    const int firstSongIndex = 0;
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });

    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.LoadSongs();

    // Act
    viewModel.SelectedSong = viewModel.Songs[firstSongIndex];
    var firstNoteMs = viewModel.GetFirstNoteStartMs();

    // Assert
    Assert.That(viewModel.LibraryPath, Is.EqualTo(songsPath));
    Assert.That(firstNoteMs, Is.EqualTo(expectedFirstNoteMs));
  }

  [Test]
  public void SelectedSong_NoNotes_ReturnsNullFirstNoteStartMs()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:1000\n- 4\nE";
    const int firstSongIndex = 0;
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });

    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.LoadSongs();

    // Act
    viewModel.SelectedSong = viewModel.Songs[firstSongIndex];
    var firstNoteMs = viewModel.GetFirstNoteStartMs();

    // Assert
    Assert.That(firstNoteMs, Is.Null);
  }

  [Test]
  public void SelectedSong_WithCover_SetsCoverPath()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string coverFile = @"c:\app\songs\SongA\cover.png";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:1000\n#COVER:cover.png\n: 8 2 0 hi\nE";
    const int firstSongIndex = 0;
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { coverFile, new MockFileData(string.Empty) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });

    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.LoadSongs();

    // Act
    viewModel.SelectedSong = viewModel.Songs[firstSongIndex];

    // Assert
    Assert.That(viewModel.SelectedSongCoverPath, Is.EqualTo(coverFile));
    Assert.That(viewModel.SelectedSongCoverDisplay, Is.EqualTo(string.Empty));
  }

  [Test]
  public void SelectedSong_NoCover_SetsEmptyCoverPath()
  {
    // Arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:1000\n: 8 2 0 hi\nE";
    const string expectedCoverPath = "";
    var expectedCoverDisplay = KaraokePlayer.Resources.Strings.CoverPlaceholder;
    const int firstSongIndex = 0;
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });

    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.LoadSongs();

    // Act
    viewModel.SelectedSong = viewModel.Songs[firstSongIndex];

    // Assert
    Assert.That(viewModel.SelectedSongCoverPath, Is.EqualTo(expectedCoverPath));
    Assert.That(viewModel.SelectedSongCoverDisplay, Is.EqualTo(expectedCoverDisplay));
  }

  [Test]
  public void NavigateBack_FromOptions_ShowsMainMenu()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.ShowOptions();

    // Act
    viewModel.NavigateBack();

    // Assert
    Assert.That(viewModel.CurrentView, Is.InstanceOf<MainMenuViewModel>());
  }

  [Test]
  public void NavigateBack_FromGame_ShowsQueue()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.ShowGame();

    // Act
    viewModel.NavigateBack();

    // Assert
    Assert.That(viewModel.CurrentView, Is.InstanceOf<QueueViewModel>());
  }

  [Test]
  public void NavigateBack_FromQueue_ShowsMainMenu()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.ShowQueue();

    // Act
    viewModel.NavigateBack();

    // Assert
    Assert.That(viewModel.CurrentView, Is.InstanceOf<MainMenuViewModel>());
  }

  [Test]
  public void NavigateBack_FromMainMenu_DoesNothing()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    var initialView = viewModel.CurrentView;

    // Act
    viewModel.NavigateBack();

    // Assert
    Assert.That(viewModel.CurrentView, Is.SameAs(initialView));
    Assert.That(viewModel.CurrentView, Is.InstanceOf<MainMenuViewModel>());
  }

  [Test]
  public void CanNavigateBack_MainMenu_ReturnsFalse()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);

    // Act
    var canNavigate = viewModel.CanNavigateBack();

    // Assert
    Assert.That(canNavigate, Is.False);
  }

  [Test]
  public void CanNavigateBack_Options_ReturnsTrue()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.ShowOptions();

    // Act
    var canNavigate = viewModel.CanNavigateBack();

    // Assert
    Assert.That(canNavigate, Is.True);
  }

  [Test]
  public void CanNavigateBack_Queue_ReturnsTrue()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.ShowQueue();

    // Act
    var canNavigate = viewModel.CanNavigateBack();

    // Assert
    Assert.That(canNavigate, Is.True);
  }

  [Test]
  public void CanNavigateBack_Game_ReturnsTrue()
  {
    // Arrange
    const string basePath = @"c:\app";
    var fileSystemMock = new MockFileSystem();
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    viewModel.ShowGame();

    // Act
    var canNavigate = viewModel.CanNavigateBack();

    // Assert
    Assert.That(canNavigate, Is.True);
  }

  [Test]
  public void RemoveFromQueue_ItemExists_RemovesItem()
  {
    // arrange
    var viewModel = new MainViewModel();
    var song = new KaraokeCore.Library.SongEntry("c:\\song", "c:\\song.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), "c:\\song.mp3", null, null, null);
    viewModel.Queue.Add(song);

    // act
    var removed = viewModel.RemoveFromQueue(song);

    // assert
    Assert.That(removed, Is.True);
    Assert.That(viewModel.Queue.Count, Is.EqualTo(0));
  }

  [Test]
  public void MoveQueueItemUp_ItemIsNotFirst_MovesItemUp()
  {
    // arrange
    var viewModel = new MainViewModel();
    var first = new KaraokeCore.Library.SongEntry("c:\\a", "c:\\a.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), "c:\\a.mp3", null, null, null);
    var second = new KaraokeCore.Library.SongEntry("c:\\b", "c:\\b.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), "c:\\b.mp3", null, null, null);
    viewModel.Queue.Add(first);
    viewModel.Queue.Add(second);

    // act
    var moved = viewModel.MoveQueueItemUp(second);

    // assert
    Assert.That(moved, Is.True);
    Assert.That(viewModel.Queue[0], Is.SameAs(second));
    Assert.That(viewModel.Queue[1], Is.SameAs(first));
  }

  [Test]
  public void MoveQueueItemDown_ItemIsNotLast_MovesItemDown()
  {
    // arrange
    var viewModel = new MainViewModel();
    var first = new KaraokeCore.Library.SongEntry("c:\\a", "c:\\a.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), "c:\\a.mp3", null, null, null);
    var second = new KaraokeCore.Library.SongEntry("c:\\b", "c:\\b.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), "c:\\b.mp3", null, null, null);
    viewModel.Queue.Add(first);
    viewModel.Queue.Add(second);

    // act
    var moved = viewModel.MoveQueueItemDown(first);

    // assert
    Assert.That(moved, Is.True);
    Assert.That(viewModel.Queue[0], Is.SameAs(second));
    Assert.That(viewModel.Queue[1], Is.SameAs(first));
  }

  [Test]
  public void StartKaraoke_WithQueue_RemovesFirstAndSetsCurrentSong()
  {
    // arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE";
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });
    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    var song = new KaraokeCore.Library.SongEntry(songFolder, $@"{songFolder}\song.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), $@"{songFolder}\track.mp3", null, null, null);
    viewModel.Queue.Add(song);

    // act
    viewModel.StartKaraoke();

    // assert
    Assert.That(viewModel.CurrentQueueSong, Is.SameAs(song));
    Assert.That(viewModel.Queue.Count, Is.EqualTo(0));
    Assert.That(viewModel.CurrentView, Is.InstanceOf<QueueViewModel>().Or.InstanceOf<GameViewModel>());
  }

  [Test]
  public void PlayNextQueueSong_WithItems_RemovesFirstAndUpdatesCurrentSong()
  {
    // arrange
    const string basePath = @"c:\app";
    const string songFolder = @"c:\app\songs\SongA";
    const string songTxt = "#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE";
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData(songTxt) },
      { $@"{songFolder}\track.mp3", new MockFileData(string.Empty) },
    });
    fileSystemMock.AddFile($@"{basePath}\karaoke.settings.json", new MockFileData("{\"LanguageCode\":\"en-US\",\"WindowMode\":\"BorderlessFullscreen\"}"));
    var settingsService = new KaraokePlayer.Configuration.SettingsService(basePath, fileSystemMock);
    var viewModel = new MainViewModel(basePath, fileSystemMock, settingsService);
    var first = new KaraokeCore.Library.SongEntry(songFolder, $@"{songFolder}\song.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), $@"{songFolder}\track.mp3", null, null, null);
    var second = new KaraokeCore.Library.SongEntry(songFolder, $@"{songFolder}\song.txt", new KaraokeCore.Models.SongMetadata(new Dictionary<string, string>()), $@"{songFolder}\track.mp3", null, null, null);
    viewModel.Queue.Add(first);
    viewModel.Queue.Add(second);

    // act
    var advanced = viewModel.PlayNextQueueSong();

    // assert
    Assert.That(advanced, Is.True);
    Assert.That(viewModel.CurrentQueueSong, Is.SameAs(first));
    Assert.That(viewModel.Queue.Count, Is.EqualTo(1));
    Assert.That(viewModel.Queue[0], Is.SameAs(second));
  }

  [Test]
  public void PlayNextQueueSong_WithEmptyQueue_ReturnsFalseAndClearsCurrentSong()
  {
    // arrange
    var viewModel = new MainViewModel();

    // act
    var advanced = viewModel.PlayNextQueueSong();

    // assert
    Assert.That(advanced, Is.False);
    Assert.That(viewModel.CurrentQueueSong, Is.Null);
  }
}
