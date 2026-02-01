using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace KaraokePlayer.Views;

public partial class GameView : System.Windows.Controls.UserControl
{
  private LibVLCSharp.Shared.LibVLC? _libVlc;
  private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
  private DispatcherTimer? _lyricTimer;
  private Window? _hostWindow;
  private GameOverlayWindow? _gameOverlay;
  private KaraokePlayer.Presentation.GameViewModel? _viewModel;
  private long _lastKnownTimeMs;
  private DateTime _lastTimeSyncUtc;
  private const long SkipMinimumLeadMs = 3000;
  private const long SkipOffsetMs = 2000;

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
    _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);
    GameSurface.MediaPlayer = _mediaPlayer;

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

    if (_mediaPlayer is not null)
    {
      _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
      _mediaPlayer.EndReached += MediaPlayer_EndReached;
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
    }
    Dispatcher.BeginInvoke(UpdateOverlayPlacement, DispatcherPriority.Loaded);
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    GameSurface.MediaPlayer = null;
    var mediaPlayer = _mediaPlayer;
    var libVlc = _libVlc;
    _mediaPlayer = null;
    _libVlc = null;

    if (mediaPlayer is not null)
    {
      mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
      mediaPlayer.EndReached -= MediaPlayer_EndReached;
    }

    if (_lyricTimer is not null)
    {
      _lyricTimer.Stop();
      _lyricTimer.Tick -= LyricTimer_Tick;
      _lyricTimer = null;
    }

    LayoutUpdated -= Overlay_LayoutUpdated;
    GameSurface.SizeChanged -= Overlay_SizeChanged;
    if (_hostWindow is not null)
    {
      _hostWindow.LocationChanged -= HostWindow_LocationChanged;
      _hostWindow.SizeChanged -= HostWindow_SizeChanged;
      _hostWindow = null;
    }

    if (_gameOverlay is not null)
    {
      _gameOverlay.Loaded -= LyricsOverlay_Loaded;
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

    if (mediaPlayer is null && libVlc is null)
    {
      return;
    }

    _ = Task.Run(() =>
    {
      mediaPlayer?.Stop();
      mediaPlayer?.Dispose();
      libVlc?.Dispose();
    });
  }

  private void PlaySong(KaraokePlayer.Presentation.GameViewModel viewModel)
  {
    if (_mediaPlayer is null || _libVlc is null)
    {
      return;
    }

    var song = viewModel.CurrentSong;
    var hasVideo = !string.IsNullOrWhiteSpace(song?.VideoPath);
    var path = hasVideo ? song?.VideoPath : song?.AudioPath;
    if (string.IsNullOrWhiteSpace(path))
    {
      _mediaPlayer.Stop();
      GameSurface.MediaPlayer = null;
      GameSurface.Visibility = Visibility.Collapsed;
      return;
    }

    if (hasVideo)
    {
      if (GameSurface.MediaPlayer is null)
      {
        GameSurface.MediaPlayer = _mediaPlayer;
      }
      GameSurface.Visibility = Visibility.Visible;
    }
    else
    {
      GameSurface.MediaPlayer = null;
      GameSurface.Visibility = Visibility.Collapsed;
    }

    using var media = new LibVLCSharp.Shared.Media(_libVlc, path, LibVLCSharp.Shared.FromType.FromPath);
    if (hasVideo && !string.IsNullOrWhiteSpace(song?.AudioPath))
    {
      var audioMrl = new Uri(song.AudioPath).AbsoluteUri;
      media.AddOption($":input-slave={audioMrl}");
    }
    _mediaPlayer.Play(media);
  }

  private void MediaPlayer_TimeChanged(object? sender, LibVLCSharp.Shared.MediaPlayerTimeChangedEventArgs e)
  {
    var currentMs = e.Time;
    _lastKnownTimeMs = currentMs;
    _lastTimeSyncUtc = DateTime.UtcNow;
    Dispatcher.BeginInvoke(() =>
    {
      if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
      {
        viewModel.UpdateLyricDisplay(currentMs);
        viewModel.UpdatePlaybackProgress(currentMs, _mediaPlayer?.Length ?? 0);
      }
    });
  }

  private void MediaPlayer_EndReached(object? sender, EventArgs e)
  {
    Dispatcher.BeginInvoke(() =>
    {
      if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
      {
        var advanced = viewModel.TryAdvanceQueue();
        if (advanced)
        {
          _lastKnownTimeMs = 0;
          _lastTimeSyncUtc = DateTime.UtcNow;
          PlaySong(viewModel);
          viewModel.EnsureLyricsLoaded();
          viewModel.UpdateLyricDisplay(0);
        }
      }
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
    if (_mediaPlayer is null)
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

    var currentMs = _mediaPlayer.Time;
    var leadMs = (long)firstNoteMs.Value - currentMs;
    if (leadMs <= SkipMinimumLeadMs)
    {
      return;
    }

    var targetMs = Math.Max(0, (long)firstNoteMs.Value - SkipOffsetMs);
    _mediaPlayer.Time = targetMs;
    _lastKnownTimeMs = targetMs;
    _lastTimeSyncUtc = DateTime.UtcNow;
    viewModel.UpdateLyricDisplay(targetMs);
    viewModel.UpdatePlaybackProgress(targetMs, _mediaPlayer.Length);
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
    if (_mediaPlayer is null)
    {
      return;
    }

    var nowUtc = DateTime.UtcNow;
    var baseTimeMs = _lastKnownTimeMs == 0 ? _mediaPlayer.Time : _lastKnownTimeMs;
    var elapsedMs = (nowUtc - _lastTimeSyncUtc).TotalMilliseconds;
    var estimatedMs = baseTimeMs + elapsedMs;

    if (DataContext is KaraokePlayer.Presentation.GameViewModel viewModel)
    {
      viewModel.UpdateLyricDisplay(estimatedMs);
      viewModel.UpdatePlaybackProgress(estimatedMs, _mediaPlayer.Length);
    }
  }
}
