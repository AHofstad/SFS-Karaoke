using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace KaraokePlayer.Views;

public partial class GameView : System.Windows.Controls.UserControl
{
  private LibVLCSharp.Shared.LibVLC? _libVlc;
  private LibVLCSharp.Shared.MediaPlayer? _audioPlayer;
  private LibVLCSharp.Shared.MediaPlayer? _videoPlayer;
  private int _videoGapMs;
  private DispatcherTimer? _lyricTimer;
  private Window? _hostWindow;
  private GameOverlayWindow? _gameOverlay;
  private KaraokePlayer.Presentation.GameViewModel? _viewModel;
  private long _lastKnownTimeMs;
  private DateTime _lastTimeSyncUtc;
  private bool _isScrubbing;
  private bool _isPausedByMenu;
  private DispatcherTimer? _nextSongDelayTimer;
  private bool _isNextSongDelayPending;
  private int _nextSongCountdownSeconds;
  private const long _skipMinimumLeadMs = 3000;
  private const long _skipOffsetMs = 2000;
  private const long _maxVideoDriftMs = 80;
  private const int _nextSongDelaySeconds = 10;
  private const int _upcomingPreviewCount = 5;

  public GameView()
  {
    InitializeComponent();
    Loaded += OnLoaded;
    Unloaded += OnUnloaded;
    DataContextChanged += GameView_DataContextChanged;
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    if (_libVlc is not null)
    {
      return;
    }

    _libVlc = new LibVLCSharp.Shared.LibVLC();
    _audioPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);
    _videoPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc)
    {
      Mute = true
    };
    GameSurface.MediaPlayer = _videoPlayer;

    if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      _viewModel = viewModel;
      if (_viewModel is System.ComponentModel.INotifyPropertyChanged notifier)
      {
        notifier.PropertyChanged += ViewModel_PropertyChanged;
      }
      _viewModel.SkipToFirstNoteRequested += ViewModel_SkipToFirstNoteRequested;
      PlaySong(viewModel);
      viewModel.EnsureLyricsLoaded();
      viewModel.UpdateLyricDisplay(0);
    }

    if (_audioPlayer is not null)
    {
      _audioPlayer.TimeChanged += MediaPlayer_TimeChanged;
      _audioPlayer.EndReached += MediaPlayer_EndReached;
    }

    _hostWindow = Window.GetWindow(this);
    if (_gameOverlay is null)
    {
      _gameOverlay = new GameOverlayWindow
      {
        Owner = _hostWindow,
        DataContext = DataContext
      };
      _gameOverlay.Loaded += LyricsOverlay_Loaded;
      _gameOverlay.ScrubStarted += GameOverlay_ScrubStarted;
      _gameOverlay.ScrubCompleted += GameOverlay_ScrubCompleted;
      _gameOverlay.PauseResumeRequested += GameOverlay_PauseResumeRequested;
      _gameOverlay.PauseRestartRequested += GameOverlay_PauseRestartRequested;
      _gameOverlay.PauseQuitRequested += GameOverlay_PauseQuitRequested;
      _gameOverlay.Show();
    }

    _lyricTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromMilliseconds(10)
    };
    _lyricTimer.Tick += LyricTimer_Tick;
    _lyricTimer.Start();

    LayoutUpdated += Overlay_LayoutUpdated;
    GameSurface.SizeChanged += Overlay_SizeChanged;
    if (_hostWindow is not null)
    {
      _hostWindow.LocationChanged += HostWindow_LocationChanged;
      _hostWindow.SizeChanged += HostWindow_SizeChanged;
      _hostWindow.Activated += HostWindow_Activated;
      _hostWindow.Deactivated += HostWindow_Deactivated;
    }
    Dispatcher.BeginInvoke(UpdateOverlayPlacement, DispatcherPriority.Loaded);
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    GameSurface.MediaPlayer = null;
    var audioPlayer = _audioPlayer;
    var videoPlayer = _videoPlayer;
    var libVlc = _libVlc;
    _audioPlayer = null;
    _videoPlayer = null;
    _libVlc = null;

    if (audioPlayer is not null)
    {
      audioPlayer.TimeChanged -= MediaPlayer_TimeChanged;
      audioPlayer.EndReached -= MediaPlayer_EndReached;
    }

    if (_lyricTimer is not null)
    {
      _lyricTimer.Stop();
      _lyricTimer.Tick -= LyricTimer_Tick;
      _lyricTimer = null;
    }

    CancelNextSongDelay();

    LayoutUpdated -= Overlay_LayoutUpdated;
    GameSurface.SizeChanged -= Overlay_SizeChanged;
    if (_hostWindow is not null)
    {
      _hostWindow.LocationChanged -= HostWindow_LocationChanged;
      _hostWindow.SizeChanged -= HostWindow_SizeChanged;
      _hostWindow.Activated -= HostWindow_Activated;
      _hostWindow.Deactivated -= HostWindow_Deactivated;
      _hostWindow = null;
    }

    if (_gameOverlay is not null)
    {
      _gameOverlay.Loaded -= LyricsOverlay_Loaded;
      _gameOverlay.ScrubStarted -= GameOverlay_ScrubStarted;
      _gameOverlay.ScrubCompleted -= GameOverlay_ScrubCompleted;
      _gameOverlay.PauseResumeRequested -= GameOverlay_PauseResumeRequested;
      _gameOverlay.PauseRestartRequested -= GameOverlay_PauseRestartRequested;
      _gameOverlay.PauseQuitRequested -= GameOverlay_PauseQuitRequested;
      _gameOverlay.Close();
      _gameOverlay = null;
    }

    if (_viewModel is System.ComponentModel.INotifyPropertyChanged viewModelNotifier)
    {
      viewModelNotifier.PropertyChanged -= ViewModel_PropertyChanged;
    }
    if (_viewModel is not null)
    {
      _viewModel.SkipToFirstNoteRequested -= ViewModel_SkipToFirstNoteRequested;
    }
    _viewModel = null;

    if (audioPlayer is null && videoPlayer is null && libVlc is null)
    {
      return;
    }

    _ = Task.Run(() =>
    {
      audioPlayer?.Stop();
      audioPlayer?.Dispose();
      videoPlayer?.Stop();
      videoPlayer?.Dispose();
      libVlc?.Dispose();
    });
  }

  private void PlaySong(KaraokePlayer.Presentation.GameViewModel viewModel)
  {
    if (_audioPlayer is null || _videoPlayer is null || _libVlc is null)
    {
      return;
    }

    viewModel.HideUpcomingQueuePreview();
    _isNextSongDelayPending = false;

    var song = viewModel.CurrentSong;
    _videoGapMs = song?.Metadata?.VideoGapMs ?? 0;
    var audioPath = song?.AudioPath;
    var hasVideo = !string.IsNullOrWhiteSpace(song?.VideoPath);
    var videoPath = song?.VideoPath;
    if (string.IsNullOrWhiteSpace(audioPath))
    {
      _audioPlayer.Stop();
      _videoPlayer.Stop();
      GameSurface.MediaPlayer = null;
      GameSurface.Visibility = Visibility.Collapsed;
      return;
    }

    if (hasVideo)
    {
      GameSurface.MediaPlayer = _videoPlayer;
      GameSurface.Visibility = Visibility.Visible;
    }
    else
    {
      GameSurface.MediaPlayer = _videoPlayer;
      GameSurface.Visibility = Visibility.Hidden;
      _videoPlayer.Stop();
      _videoPlayer.Media = null;
    }

    using var audioMedia = new LibVLCSharp.Shared.Media(_libVlc, audioPath, LibVLCSharp.Shared.FromType.FromPath);
    _audioPlayer.Play(audioMedia);

    if (hasVideo && !string.IsNullOrWhiteSpace(videoPath))
    {
      _videoPlayer.Stop();
      _videoPlayer.Media = null;
      using var videoMedia = new LibVLCSharp.Shared.Media(_libVlc, videoPath, LibVLCSharp.Shared.FromType.FromPath);
      _videoPlayer.Play(videoMedia);
      SyncVideoToAudio(_audioPlayer.Time);
    }
    else
    {
      _videoPlayer.Stop();
      _videoPlayer.Media = null;
    }
  }

  private void MediaPlayer_TimeChanged(object? sender, LibVLCSharp.Shared.MediaPlayerTimeChangedEventArgs e)
  {
    var currentMs = e.Time;
    _lastKnownTimeMs = currentMs;
    _lastTimeSyncUtc = DateTime.UtcNow;
    Dispatcher.BeginInvoke(() =>
    {
      SyncVideoToAudio(currentMs);
      if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
      {
        viewModel.UpdateLyricDisplay(currentMs);
        if (!_isScrubbing)
        {
          viewModel.UpdatePlaybackProgress(currentMs, _audioPlayer?.Length ?? 0);
        }
      }
    });
  }

  private void MediaPlayer_EndReached(object? sender, EventArgs e)
  {
    Dispatcher.BeginInvoke(() =>
    {
      if (DataContext is not KaraokePlayer.Presentation.GameViewModel viewModel || _isNextSongDelayPending)
      {
        return;
      }

      _audioPlayer?.Stop();
      _videoPlayer?.Stop();
      _isNextSongDelayPending = true;
      viewModel.ShowUpcomingQueuePreview(_upcomingPreviewCount);
      _nextSongCountdownSeconds = _nextSongDelaySeconds;
      viewModel.UpdateUpcomingQueueCountdown(_nextSongCountdownSeconds);
      EnsureNextSongDelayTimer();
      _nextSongDelayTimer?.Start();
    });
  }

  private void GameView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (_gameOverlay is not null)
    {
      _gameOverlay.DataContext = DataContext;
    }
  }

  private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    if (e.PropertyName == nameof(KaraokePlayer.Presentation.GameViewModel.CurrentSong)
        && DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      _lastKnownTimeMs = 0;
      _lastTimeSyncUtc = DateTime.UtcNow;
      PlaySong(viewModel);
      viewModel.EnsureLyricsLoaded();
      viewModel.UpdateLyricDisplay(0);
    }
  }

  private void ViewModel_SkipToFirstNoteRequested(object? sender, EventArgs e)
  {
    if (_audioPlayer is null)
    {
      return;
    }

    if (DataContext is not KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      return;
    }

    var firstNoteMs = viewModel.FirstNoteStartMs;
    if (firstNoteMs is null)
    {
      return;
    }

    var currentMs = _audioPlayer.Time;
    var leadMs = (long)firstNoteMs.Value - currentMs;
    if (leadMs <= _skipMinimumLeadMs)
    {
      return;
    }

    var targetMs = Math.Max(0, (long)firstNoteMs.Value - _skipOffsetMs);
    _audioPlayer.Time = targetMs;
    _lastKnownTimeMs = targetMs;
    _lastTimeSyncUtc = DateTime.UtcNow;
    viewModel.UpdateLyricDisplay(targetMs);
    viewModel.UpdatePlaybackProgress(targetMs, _audioPlayer.Length);
  }

  private void LyricsOverlay_Loaded(object? sender, RoutedEventArgs e)
  {
    UpdateOverlayPlacement();
  }

  private void Overlay_LayoutUpdated(object? sender, EventArgs e)
  {
    UpdateOverlayPlacement();
  }

  private void Overlay_SizeChanged(object sender, SizeChangedEventArgs e)
  {
    UpdateOverlayPlacement();
  }

  private void HostWindow_LocationChanged(object? sender, EventArgs e)
  {
    UpdateOverlayPlacement();
  }

  private void HostWindow_SizeChanged(object sender, SizeChangedEventArgs e)
  {
    UpdateOverlayPlacement();
  }

  private void HostWindow_Activated(object? sender, EventArgs e)
  {
    UpdateOverlayActivationVisibility();
  }

  private void HostWindow_Deactivated(object? sender, EventArgs e)
  {
    Dispatcher.BeginInvoke(UpdateOverlayActivationVisibility, DispatcherPriority.ApplicationIdle);
  }

  private void UpdateOverlayActivationVisibility()
  {
    if (_gameOverlay is null)
    {
      return;
    }

    var hasActiveAppWindow = global::System.Windows.Application.Current.Windows
      .OfType<Window>()
      .Any(window => window.IsActive);

    if (hasActiveAppWindow)
    {
      if (!_gameOverlay.IsVisible)
      {
        _gameOverlay.Show();
      }

      UpdateOverlayPlacement();
      return;
    }

    _gameOverlay.Hide();
  }

  private void UpdateOverlayPlacement()
  {
    if (!IsLoaded || _gameOverlay is null)
    {
      return;
    }

    var previewHeight = ActualHeight;
    var previewWidth = ActualWidth;
    if (previewHeight <= 0 || previewWidth <= 0)
    {
      return;
    }

    var source = PresentationSource.FromVisual(this);
    if (source?.CompositionTarget is null)
    {
      return;
    }

    var topLeftScreen = PointToScreen(new System.Windows.Point(0, 0));
    var bottomRightScreen = PointToScreen(new System.Windows.Point(previewWidth, previewHeight));
    var transformFromDevice = source.CompositionTarget.TransformFromDevice;
    var topLeft = transformFromDevice.Transform(topLeftScreen);
    var bottomRight = transformFromDevice.Transform(bottomRightScreen);
    var renderedWidth = Math.Max(0, bottomRight.X - topLeft.X);
    var renderedHeight = Math.Max(0, bottomRight.Y - topLeft.Y);
    if (renderedWidth <= 0 || renderedHeight <= 0)
    {
      return;
    }

    _gameOverlay.Width = renderedWidth;
    _gameOverlay.Height = renderedHeight;
    _gameOverlay.Left = topLeft.X;
    _gameOverlay.Top = topLeft.Y;
  }

  private void LyricTimer_Tick(object? sender, EventArgs e)
  {
    if (_audioPlayer is null)
    {
      return;
    }

    var nowUtc = DateTime.UtcNow;
    var baseTimeMs = _lastKnownTimeMs == 0 ? _audioPlayer.Time : _lastKnownTimeMs;
    var elapsedMs = _isPausedByMenu ? 0 : (nowUtc - _lastTimeSyncUtc).TotalMilliseconds;
    var estimatedMs = baseTimeMs + elapsedMs;

    if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      viewModel.UpdateLyricDisplay(estimatedMs);
      if (!_isScrubbing)
      {
        viewModel.UpdatePlaybackProgress(estimatedMs, _audioPlayer.Length);
      }
    }
  }

  private void GameOverlay_ScrubStarted(object? sender, EventArgs e)
  {
    _isScrubbing = true;
  }

  private void GameOverlay_ScrubCompleted(object? sender, double progressPercent)
  {
    _isScrubbing = false;
    if (_audioPlayer is null)
    {
      return;
    }

    var durationMs = _audioPlayer.Length;
    if (durationMs <= 0)
    {
      return;
    }

    var targetMs = (long)(durationMs * (progressPercent / 100d));
    _audioPlayer.Time = targetMs;
    _lastKnownTimeMs = targetMs;
    _lastTimeSyncUtc = DateTime.UtcNow;
    SyncVideoToAudio(targetMs);

    if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      viewModel.UpdateLyricDisplay(targetMs);
      viewModel.UpdatePlaybackProgress(targetMs, durationMs);
    }
  }

  private void SyncVideoToAudio(long audioTimeMs)
  {
    if (_videoPlayer is null)
    {
      return;
    }

    if (_videoPlayer.Media is null)
    {
      return;
    }

    var videoTimeMs = audioTimeMs - _videoGapMs;
    if (videoTimeMs < 0)
    {
      GameSurface.Visibility = Visibility.Hidden;
      if (_videoPlayer.IsPlaying)
      {
        _videoPlayer.Pause();
      }
      _videoPlayer.Time = 0;
      return;
    }

    GameSurface.Visibility = Visibility.Visible;
    if (!_videoPlayer.IsPlaying)
    {
      _videoPlayer.Play();
    }

    var drift = Math.Abs(_videoPlayer.Time - videoTimeMs);
    if (drift > _maxVideoDriftMs)
    {
      _videoPlayer.Time = videoTimeMs;
    }
  }

  private void EnsureNextSongDelayTimer()
  {
    if (_nextSongDelayTimer is not null)
    {
      return;
    }

    _nextSongDelayTimer = new DispatcherTimer
    {
      Interval = TimeSpan.FromSeconds(1)
    };
    _nextSongDelayTimer.Tick += NextSongDelayTimer_Tick;
  }

  private void NextSongDelayTimer_Tick(object? sender, EventArgs e)
  {
    if (DataContext is not KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      return;
    }

    _nextSongCountdownSeconds--;
    viewModel.UpdateUpcomingQueueCountdown(_nextSongCountdownSeconds);
    if (_nextSongCountdownSeconds > 0)
    {
      return;
    }

    AdvanceAfterUpcomingQueuePreview(viewModel);
  }

  private void CancelNextSongDelay()
  {
    if (_nextSongDelayTimer is not null)
    {
      _nextSongDelayTimer.Stop();
      _nextSongDelayTimer.Tick -= NextSongDelayTimer_Tick;
      _nextSongDelayTimer = null;
    }

    _isNextSongDelayPending = false;
    _nextSongCountdownSeconds = 0;
    if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      viewModel.HideUpcomingQueuePreview();
    }
  }

  private void AdvanceAfterUpcomingQueuePreview(KaraokePlayer.Presentation.GameViewModel viewModel)
  {
    _nextSongDelayTimer?.Stop();
    _isNextSongDelayPending = false;
    _nextSongCountdownSeconds = 0;

    viewModel.HideUpcomingQueuePreview();
    var advanced = viewModel.TryAdvanceQueue();
    if (!advanced)
    {
      return;
    }

    _lastKnownTimeMs = 0;
    _lastTimeSyncUtc = DateTime.UtcNow;
    PlaySong(viewModel);
    viewModel.EnsureLyricsLoaded();
    viewModel.UpdateLyricDisplay(0);
  }

  public bool TryAdvanceUpcomingQueueNow()
  {
    if (!_isNextSongDelayPending || DataContext is not KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      return false;
    }

    AdvanceAfterUpcomingQueuePreview(viewModel);
    return true;
  }

  public bool TrySeekRelativeSeconds(int deltaSeconds)
  {
    if (_audioPlayer is null || DataContext is not KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      return false;
    }

    if (_isNextSongDelayPending)
    {
      return false;
    }

    var durationMs = _audioPlayer.Length;
    if (durationMs <= 0)
    {
      return false;
    }

    var targetMs = _audioPlayer.Time + deltaSeconds * 1000L;
    targetMs = Math.Max(0, Math.Min(durationMs, targetMs));
    _audioPlayer.Time = targetMs;
    _lastKnownTimeMs = targetMs;
    _lastTimeSyncUtc = DateTime.UtcNow;
    SyncVideoToAudio(targetMs);
    viewModel.UpdateLyricDisplay(targetMs);
    viewModel.UpdatePlaybackProgress(targetMs, durationMs);
    return true;
  }

  public bool TogglePauseMenu()
  {
    if (_gameOverlay is null)
    {
      return false;
    }

    if (_gameOverlay.IsPauseMenuVisible)
    {
      ResumeFromPauseMenu();
      return true;
    }

    OpenPauseMenu();
    return true;
  }

  private void OpenPauseMenu()
  {
    if (_audioPlayer is not null)
    {
      _lastKnownTimeMs = _audioPlayer.Time;
      _lastTimeSyncUtc = DateTime.UtcNow;
      _audioPlayer.Pause();
    }

    _videoPlayer?.Pause();
    _isPausedByMenu = true;
    _gameOverlay?.ShowPauseMenu();
  }

  private void ResumeFromPauseMenu()
  {
    _gameOverlay?.HidePauseMenu();
    _isPausedByMenu = false;

    if (_audioPlayer is not null)
    {
      _audioPlayer.Play();
      _lastKnownTimeMs = _audioPlayer.Time;
      _lastTimeSyncUtc = DateTime.UtcNow;
      SyncVideoToAudio(_audioPlayer.Time);
    }

    RestoreMainWindowFocus();
  }

  private void RestartFromPauseMenu()
  {
    if (_audioPlayer is null)
    {
      return;
    }

    CancelNextSongDelay();

    _audioPlayer.Time = 0;
    _lastKnownTimeMs = 0;
    _lastTimeSyncUtc = DateTime.UtcNow;
    _isPausedByMenu = false;
    _gameOverlay?.HidePauseMenu();
    SyncVideoToAudio(0);
    _audioPlayer.Play();

    if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      viewModel.UpdateLyricDisplay(0);
      viewModel.UpdatePlaybackProgress(0, _audioPlayer.Length);
    }

    RestoreMainWindowFocus();
  }

  private void QuitFromPauseMenu()
  {
    CancelNextSongDelay();
    GameSurface.MediaPlayer = null;
    GameSurface.Visibility = Visibility.Hidden;
    _audioPlayer?.Stop();
    _videoPlayer?.Stop();
    _isPausedByMenu = false;
    _gameOverlay?.HidePauseMenu();

    if (global::System.Windows.Application.Current.MainWindow?.DataContext is KaraokePlayer.Presentation.MainViewModel rootViewModel)
    {
      rootViewModel.NavigateBack();
    }

    RestoreMainWindowFocus();
  }

  private void GameOverlay_PauseResumeRequested(object? sender, EventArgs e)
  {
    ResumeFromPauseMenu();
  }

  private void GameOverlay_PauseRestartRequested(object? sender, EventArgs e)
  {
    RestartFromPauseMenu();
  }

  private void GameOverlay_PauseQuitRequested(object? sender, EventArgs e)
  {
    QuitFromPauseMenu();
  }

  private void RestoreMainWindowFocus()
  {
    var targetWindow = _hostWindow ?? global::System.Windows.Application.Current.MainWindow;
    if (targetWindow is null)
    {
      return;
    }

    Dispatcher.BeginInvoke(() =>
    {
      targetWindow.Activate();
      Keyboard.Focus(targetWindow);
      Focus();
    }, DispatcherPriority.ApplicationIdle);
  }
}
