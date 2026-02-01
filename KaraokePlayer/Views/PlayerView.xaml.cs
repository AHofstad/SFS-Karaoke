using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace KaraokePlayer.Views;

public partial class PlayerView : System.Windows.Controls.UserControl
{
  private LibVLCSharp.Shared.LibVLC? _libVlc;
  private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;

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
    _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVlc);
    VideoSurface.MediaPlayer = _mediaPlayer;

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
    var mediaPlayer = _mediaPlayer;
    var libVlc = _libVlc;
    _mediaPlayer = null;
    _libVlc = null;

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
    if (_mediaPlayer is null || _libVlc is null)
    {
      return;
    }

    var song = viewModel.SelectedSong;
    var path = song?.VideoPath ?? song?.AudioPath;
    if (string.IsNullOrWhiteSpace(path))
    {
      _mediaPlayer.Stop();
      return;
    }

    using var media = new LibVLCSharp.Shared.Media(_libVlc, path, LibVLCSharp.Shared.FromType.FromPath);
    _mediaPlayer.Play(media);
  }
}
