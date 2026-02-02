using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace KaraokePlayer.Views;

public partial class PlayerView : System.Windows.Controls.UserControl
{
  private LibVLCSharp.Shared.LibVLC? _libVlc;
  private LibVLCSharp.Shared.MediaPlayer? _audioPlayer;
  private LibVLCSharp.Shared.MediaPlayer? _videoPlayer;
  private int _videoGapMs;
  private const long MaxVideoDriftMs = 80;

  public PlayerView()
  {
    InitializeComponent();
    Loaded += OnLoaded;
    Unloaded += OnUnloaded;
  }

  public static readonly RoutedEvent ReloadLibraryRequestedEvent =
    EventManager.RegisterRoutedEvent(
      nameof(ReloadLibraryRequested),
      RoutingStrategy.Bubble,
      typeof(RoutedEventHandler),
      typeof(PlayerView));

  public event RoutedEventHandler ReloadLibraryRequested
  {
    add => AddHandler(ReloadLibraryRequestedEvent, value);
    remove => RemoveHandler(ReloadLibraryRequestedEvent, value);
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
    VideoSurface.MediaPlayer = _videoPlayer;
    _audioPlayer.TimeChanged += AudioPlayer_TimeChanged;

    if (DataContext is KaraokePlayer.Presentation.MainViewModel viewModel)
    {
      viewModel.PropertyChanged += ViewModel_PropertyChanged;
      PlaySelected(viewModel);
    }
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    if (DataContext is KaraokePlayer.Presentation.MainViewModel viewModel)
    {
      viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    VideoSurface.MediaPlayer = null;
    var audioPlayer = _audioPlayer;
    var videoPlayer = _videoPlayer;
    var libVlc = _libVlc;
    _audioPlayer = null;
    _videoPlayer = null;
    _libVlc = null;

    if (audioPlayer is not null)
    {
      audioPlayer.TimeChanged -= AudioPlayer_TimeChanged;
    }

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

  private void ReloadLibrary_Click(object sender, RoutedEventArgs e)
  {
    RaiseEvent(new RoutedEventArgs(ReloadLibraryRequestedEvent));
  }

  private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    const string selectedSongProperty = nameof(KaraokePlayer.Presentation.MainViewModel.SelectedSong);
    if (e.PropertyName == selectedSongProperty && sender is KaraokePlayer.Presentation.MainViewModel viewModel)
    {
      PlaySelected(viewModel);
    }
  }

  private void PlaySelected(KaraokePlayer.Presentation.MainViewModel viewModel)
  {
    if (_audioPlayer is null || _videoPlayer is null || _libVlc is null)
    {
      return;
    }

    var song = viewModel.SelectedSong;
    _videoGapMs = song?.Metadata?.VideoGapMs ?? 0;
    var audioPath = song?.AudioPath;
    var hasVideo = !string.IsNullOrWhiteSpace(song?.VideoPath);
    var videoPath = song?.VideoPath;
    if (string.IsNullOrWhiteSpace(audioPath))
    {
      _audioPlayer.Stop();
      _videoPlayer.Stop();
      VideoSurface.MediaPlayer = null;
      return;
    }

    if (hasVideo)
    {
      VideoSurface.MediaPlayer = _videoPlayer;
      VideoSurface.Visibility = Visibility.Visible;
    }
    else
    {
      VideoSurface.MediaPlayer = _videoPlayer;
      VideoSurface.Visibility = Visibility.Hidden;
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

  private void AudioPlayer_TimeChanged(object? sender, LibVLCSharp.Shared.MediaPlayerTimeChangedEventArgs e)
  {
    Dispatcher.BeginInvoke(() => SyncVideoToAudio(e.Time));
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
      VideoSurface.Visibility = Visibility.Hidden;
      if (_videoPlayer.IsPlaying)
      {
        _videoPlayer.Pause();
      }
      _videoPlayer.Time = 0;
      return;
    }

    VideoSurface.Visibility = Visibility.Visible;
    if (!_videoPlayer.IsPlaying)
    {
      _videoPlayer.Play();
    }

    var drift = Math.Abs(_videoPlayer.Time - videoTimeMs);
    if (drift > MaxVideoDriftMs)
    {
      _videoPlayer.Time = videoTimeMs;
    }
  }
}
