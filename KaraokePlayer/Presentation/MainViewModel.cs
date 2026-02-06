using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using KaraokeCore.Library;
using KaraokeCore.Parsing;
using KaraokeCore.Timing;
using KaraokeCore.Queue;

namespace KaraokePlayer.Presentation;

public sealed class MainViewModel : INotifyPropertyChanged
{
  private const string SongsFolderName = "songs";
  private const int ProgressStart = 0;
  private const int ProgressComplete = 100;
  private readonly string _basePath;
  private readonly System.IO.Abstractions.IFileSystem _fileSystem;
  private readonly Configuration.SettingsService _settingsService;
  private readonly Dispatcher? _dispatcher;

  private string _status = KaraokePlayer.Resources.Strings.StatusLoading;
  private string _libraryPath = string.Empty;
  private SongEntry? _selectedSong;
  private double _loadingProgress;
  private string _remainingTime = "--:--";
  private double? _firstNoteStartMs;
  private string _loadingCount = string.Empty;
  private string _selectedSongTitle = "--";
  private string _selectedSongArtist = "--";
  private string _selectedSongAudio = "--";
  private string _selectedSongVideo = "--";
  private string _selectedSongBpm = "--";
  private string _selectedSongCoverPath = string.Empty;
  private string _selectedSongCoverDisplay = string.Empty;
  private string _selectedSongBackgroundPath = string.Empty;
  private bool _hasSelectedVideo;
  private bool _hasSelectedBackground;
  private bool _isLoadingComplete;
  private object _currentView;
  private SongEntry? _currentQueueSong;
  private bool _isLoading;

  public ObservableCollection<SongEntry> Songs { get; } = new();
  public ObservableCollection<SongEntry> Queue { get; } = new();
  public OptionsViewModel Options { get; }
  public QueueViewModel QueueView { get; }
  public GameViewModel GameView { get; }

  public object CurrentView
  {
    get => _currentView;
    private set => SetField(ref _currentView, value);
  }

  public MainMenuViewModel MainMenu { get; } = new();
  public SongEntry? CurrentQueueSong
  {
    get => _currentQueueSong;
    private set => SetField(ref _currentQueueSong, value);
  }

  public MainViewModel()
    : this(AppDomain.CurrentDomain.BaseDirectory, new System.IO.Abstractions.FileSystem(), Configuration.SettingsService.Instance)
  {
  }

  public MainViewModel(string basePath, System.IO.Abstractions.IFileSystem fileSystem, Configuration.SettingsService settingsService)
  {
    _basePath = basePath;
    _fileSystem = fileSystem;
    _settingsService = settingsService;
    _dispatcher = global::System.Windows.Application.Current?.Dispatcher;
    Options = new OptionsViewModel(settingsService);
    QueueView = new QueueViewModel(this);
    GameView = new GameViewModel(this);
    _currentView = MainMenu;
  }

  public string Status
  {
    get => _status;
    private set => SetField(ref _status, value);
  }

  public string LibraryPath
  {
    get => _libraryPath;
    private set => SetField(ref _libraryPath, value);
  }

  public SongEntry? SelectedSong
  {
    get => _selectedSong;
    set
    {
      if (SetField(ref _selectedSong, value))
      {
        UpdateFirstNoteStart();
        UpdateSelectedSongDetails();
        OnPropertyChanged(nameof(HasSelectedSong));
        OnPropertyChanged(nameof(FirstNoteStartMs));
      }
    }
  }

  public bool HasSelectedSong => SelectedSong is not null;

  public double? FirstNoteStartMs => _firstNoteStartMs;

  public string SelectedSongTitle
  {
    get => _selectedSongTitle;
    private set => SetField(ref _selectedSongTitle, value);
  }

  public string SelectedSongArtist
  {
    get => _selectedSongArtist;
    private set => SetField(ref _selectedSongArtist, value);
  }

  public string SelectedSongAudio
  {
    get => _selectedSongAudio;
    private set => SetField(ref _selectedSongAudio, value);
  }

  public string SelectedSongVideo
  {
    get => _selectedSongVideo;
    private set => SetField(ref _selectedSongVideo, value);
  }

  public string SelectedSongBpm
  {
    get => _selectedSongBpm;
    private set => SetField(ref _selectedSongBpm, value);
  }

  public string SelectedSongCoverPath
  {
    get => _selectedSongCoverPath;
    private set => SetField(ref _selectedSongCoverPath, value);
  }

  public string SelectedSongCoverDisplay
  {
    get => _selectedSongCoverDisplay;
    private set => SetField(ref _selectedSongCoverDisplay, value);
  }

  public string SelectedSongBackgroundPath
  {
    get => _selectedSongBackgroundPath;
    private set => SetField(ref _selectedSongBackgroundPath, value);
  }

  public bool HasSelectedVideo
  {
    get => _hasSelectedVideo;
    private set => SetField(ref _hasSelectedVideo, value);
  }

  public bool HasSelectedBackground
  {
    get => _hasSelectedBackground;
    private set => SetField(ref _hasSelectedBackground, value);
  }

  public double LoadingProgress
  {
    get => _loadingProgress;
    set => SetField(ref _loadingProgress, value);
  }

  public string LoadingCount
  {
    get => _loadingCount;
    private set => SetField(ref _loadingCount, value);
  }

  public bool IsLoading
  {
    get => _isLoading;
    private set => SetField(ref _isLoading, value);
  }

  public bool IsLoadingComplete
  {
    get => _isLoadingComplete;
    private set => SetField(ref _isLoadingComplete, value);
  }

  public string RemainingTime
  {
    get => _remainingTime;
    set => SetField(ref _remainingTime, value);
  }

  public event PropertyChangedEventHandler? PropertyChanged;

  public void LoadSongs()
  {
    LoadSongsAsync().GetAwaiter().GetResult();
  }

  public async Task LoadSongsAsync()
  {
    var completedSuccessfully = false;
    RunOnUiThread(() =>
    {
      Status = KaraokePlayer.Resources.Strings.StatusLoading;
      LoadingProgress = ProgressStart;
      LoadingCount = "Imported: 0";
      IsLoadingComplete = false;
      IsLoading = true;
    });

    var settings = _settingsService.Load();
    var songsPath = string.IsNullOrWhiteSpace(settings.SongsFolderPath)
      ? _fileSystem.Path.Combine(_basePath, SongsFolderName)
      : settings.SongsFolderPath;
    RunOnUiThread(() => LibraryPath = songsPath);

    try
    {
      var entries = await Task.Run(() =>
      {
        var scanner = new SongLibraryScanner(_fileSystem);
        return scanner.Scan(songsPath, loaded =>
        {
          RunOnUiThread(() =>
          {
            LoadingCount = $"Imported: {loaded}";
          });
        });
      });

      RunOnUiThread(() =>
      {
        Songs.Clear();
        foreach (var entry in entries)
        {
          Songs.Add(entry);
        }

        Status = entries.Count == 0
          ? KaraokePlayer.Resources.Strings.StatusMissing
          : KaraokePlayer.Resources.Strings.StatusLoaded;
        LoadingCount = $"Imported: {entries.Count}";
        LoadingProgress = ProgressComplete;
        IsLoadingComplete = true;
      });
      completedSuccessfully = true;
    }
    finally
    {
      if (!completedSuccessfully)
      {
        RunOnUiThread(() =>
        {
          IsLoading = false;
          IsLoadingComplete = false;
        });
      }
    }
  }

  public void DismissLoadingOverlay()
  {
    IsLoading = false;
    IsLoadingComplete = false;
  }

  public double? GetFirstNoteStartMs()
  {
    return _firstNoteStartMs;
  }

  public void ShowMainMenu()
  {
    CurrentView = MainMenu;
  }

  public void ShowOptions()
  {
    CurrentView = Options;
  }

  public void ShowQueue()
  {
    CurrentView = QueueView;
    if (SelectedSong is null && Songs.Count > 0)
    {
      SelectedSong = Songs[0];
    }
  }

  public void ShowGame()
  {
    CurrentView = GameView;
  }

  public void NavigateBack()
  {
    if (CurrentView is OptionsViewModel)
    {
      ShowMainMenu();
      return;
    }

    if (CurrentView is GameViewModel)
    {
      ShowQueue();
      return;
    }

    if (CurrentView is QueueViewModel)
    {
      ShowMainMenu();
    }
  }

  public bool CanNavigateBack()
  {
    return CurrentView is OptionsViewModel
      || CurrentView is QueueViewModel
      || CurrentView is GameViewModel;
  }

  public void SelectNextSong()
  {
    if (Songs.Count == 0)
    {
      return;
    }

    var index = SelectedSong is null ? -1 : Songs.IndexOf(SelectedSong);
    var nextIndex = (index + 1) % Songs.Count;
    SelectedSong = Songs[nextIndex];
  }

  public void SelectPreviousSong()
  {
    if (Songs.Count == 0)
    {
      return;
    }

    var index = SelectedSong is null ? 0 : Songs.IndexOf(SelectedSong);
    var prevIndex = (index - 1 + Songs.Count) % Songs.Count;
    SelectedSong = Songs[prevIndex];
  }

  public void AddSelectedToQueue()
  {
    if (SelectedSong is null)
    {
      return;
    }

    Queue.Add(SelectedSong);
  }

  public bool TryEnqueueById(string songId)
  {
    if (string.IsNullOrWhiteSpace(songId))
    {
      return false;
    }

    return RunOnUiThread(() =>
    {
      var entry = FindSongById(songId);
      if (entry is null)
      {
        return false;
      }

      Queue.Add(entry);
      return true;
    });
  }

  public IReadOnlyList<SongEntry> GetQueueSnapshot()
  {
    return RunOnUiThread(() => Queue.ToList());
  }

  public IReadOnlyList<SongEntry> GetLibrarySnapshot()
  {
    return RunOnUiThread(() => Songs.ToList());
  }

  public bool RemoveFromQueue(SongEntry? entry)
  {
    if (entry is null)
    {
      return false;
    }

    return Queue.Remove(entry);
  }

  public bool MoveQueueItemUp(SongEntry? entry)
  {
    if (entry is null)
    {
      return false;
    }

    var index = Queue.IndexOf(entry);
    if (index <= 0)
    {
      return false;
    }

    Queue.Move(index, index - 1);
    return true;
  }

  public bool MoveQueueItemDown(SongEntry? entry)
  {
    if (entry is null)
    {
      return false;
    }

    var index = Queue.IndexOf(entry);
    if (index < 0 || index >= Queue.Count - 1)
    {
      return false;
    }

    Queue.Move(index, index + 1);
    return true;
  }

  public bool CanStartKaraoke()
  {
    return Queue.Count > 0;
  }

  public bool CanAddToQueue()
  {
    return SelectedSong is not null;
  }

  public void StartKaraoke()
  {
    if (Queue.Count == 0)
    {
      return;
    }

    var song = Queue[0];
    Queue.RemoveAt(0);
    CurrentQueueSong = song;
    GameView.PrepareLyrics(song, _fileSystem);
    ShowGame();
  }

  public bool PlayNextQueueSong()
  {
    if (Queue.Count == 0)
    {
      CurrentQueueSong = null;
      return false;
    }

    var song = Queue[0];
    Queue.RemoveAt(0);
    CurrentQueueSong = song;
    GameView.PrepareLyrics(song, _fileSystem);
    return true;
  }

  private SongEntry? FindSongById(string songId)
  {
    var normalized = QueueSongId.Normalize(songId);
    foreach (var entry in Songs)
    {
      var entryId = QueueSongId.FromEntry(LibraryPath, entry);
      if (string.Equals(entryId, normalized, StringComparison.OrdinalIgnoreCase))
      {
        return entry;
      }
    }

    return null;
  }

  private T RunOnUiThread<T>(Func<T> func)
  {
    if (_dispatcher is null || _dispatcher.CheckAccess())
    {
      return func();
    }

    return _dispatcher.Invoke(func);
  }

  private void RunOnUiThread(Action action)
  {
    RunOnUiThread(() =>
    {
      action();
      return true;
    });
  }

  private void UpdateFirstNoteStart()
  {
    if (SelectedSong?.TxtPath is null)
    {
      _firstNoteStartMs = null;
      return;
    }

    var parser = new UltraStarParser();
    var lines = UltraStarTextLoader.ReadAllLines(_fileSystem, SelectedSong.TxtPath);
    var song = parser.Parse(lines);
    var timing = UltraStarTiming.TryCreate(song.Metadata);
    if (timing is null)
    {
      _firstNoteStartMs = null;
      return;
    }

    _firstNoteStartMs = timing.FirstNoteStartMs(song.Events);
  }

  private void UpdateSelectedSongDetails()
  {
    const string placeholder = "--";
    const string empty = "";
    if (SelectedSong?.Metadata is null)
    {
      SelectedSongTitle = placeholder;
      SelectedSongArtist = placeholder;
      SelectedSongAudio = placeholder;
      SelectedSongVideo = placeholder;
      SelectedSongBpm = placeholder;
      SelectedSongCoverPath = empty;
      SelectedSongCoverDisplay = KaraokePlayer.Resources.Strings.CoverPlaceholder;
      SelectedSongBackgroundPath = empty;
      HasSelectedVideo = false;
      HasSelectedBackground = false;
      return;
    }

    SelectedSongTitle = string.IsNullOrWhiteSpace(SelectedSong.Metadata.Title) ? placeholder : SelectedSong.Metadata.Title;
    SelectedSongArtist = string.IsNullOrWhiteSpace(SelectedSong.Metadata.Artist) ? placeholder : SelectedSong.Metadata.Artist;
    SelectedSongAudio = string.IsNullOrWhiteSpace(SelectedSong.Metadata.Audio) ? placeholder : SelectedSong.Metadata.Audio;
    SelectedSongVideo = string.IsNullOrWhiteSpace(SelectedSong.Metadata.Video) ? placeholder : SelectedSong.Metadata.Video;
    SelectedSongBpm = SelectedSong.Metadata.Bpm is null ? placeholder : SelectedSong.Metadata.Bpm.Value.ToString("0.##");
    SelectedSongCoverPath = SelectedSong.CoverPath ?? empty;
    SelectedSongCoverDisplay = string.IsNullOrWhiteSpace(SelectedSongCoverPath)
      ? KaraokePlayer.Resources.Strings.CoverPlaceholder
      : empty;
    SelectedSongBackgroundPath = SelectedSong.BackgroundPath ?? empty;
    HasSelectedVideo = !string.IsNullOrWhiteSpace(SelectedSong.VideoPath);
    HasSelectedBackground = !string.IsNullOrWhiteSpace(SelectedSongBackgroundPath);
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

  private void OnPropertyChanged(string propertyName)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }
}
