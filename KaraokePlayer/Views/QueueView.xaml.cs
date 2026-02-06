using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace KaraokePlayer.Views;

public partial class QueueView : System.Windows.Controls.UserControl
{
  private const double _defaultOverlayHeight = 220d;
  private const double _dragThreshold = 4d;
  private LibVLCSharp.Shared.LibVLC? _libVlc;
  private LibVLCSharp.Shared.MediaPlayer? _audioPlayer;
  private LibVLCSharp.Shared.MediaPlayer? _videoPlayer;
  private int _videoGapMs;
  private Window? _hostWindow;
  private QueueOverlayWindow? _overlayWindow;
  private System.Windows.Point _dragStartPoint;
  private const long _maxVideoDriftMs = 80;

  public QueueView()
  {
    InitializeComponent();
    Loaded += OnLoaded;
    Unloaded += OnUnloaded;
    DataContextChanged += QueueView_DataContextChanged;
  }

  public static readonly RoutedEvent StartKaraokeRequestedEvent =
    EventManager.RegisterRoutedEvent(
      nameof(StartKaraokeRequested),
      RoutingStrategy.Bubble,
      typeof(RoutedEventHandler),
      typeof(QueueView));

  public event RoutedEventHandler StartKaraokeRequested
  {
    add => AddHandler(StartKaraokeRequestedEvent, value);
    remove => RemoveHandler(StartKaraokeRequestedEvent, value);
  }

  private void StartKaraoke_Click(object sender, RoutedEventArgs e)
  {
    RaiseEvent(new RoutedEventArgs(StartKaraokeRequestedEvent));
  }

  private void QueueRemove_Click(object sender, RoutedEventArgs e)
  {
    if (DataContext is not KaraokePlayer.Presentation.QueueViewModel viewModel)
    {
      return;
    }

    if (sender is not System.Windows.Controls.Button button)
    {
      return;
    }

    viewModel.RemoveFromQueue(button.CommandParameter as KaraokeCore.Library.SongEntry);
  }

  private void QueueList_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    _dragStartPoint = e.GetPosition(null);
  }

  private void QueueList_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
  {
    if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
    {
      return;
    }

    var position = e.GetPosition(null);
    if (Math.Abs(position.X - _dragStartPoint.X) < _dragThreshold
        && Math.Abs(position.Y - _dragStartPoint.Y) < _dragThreshold)
    {
      return;
    }

    if (sender is not System.Windows.Controls.ListBox listBox)
    {
      return;
    }

    var item = GetListBoxItemUnderMouse(listBox, e.GetPosition(listBox));
    if (item is null)
    {
      return;
    }

    var data = listBox.ItemContainerGenerator.ItemFromContainer(item);
    if (data is null)
    {
      return;
    }

    DragDrop.DoDragDrop(listBox, data, System.Windows.DragDropEffects.Move);
  }

  private void QueueList_Drop(object sender, System.Windows.DragEventArgs e)
  {
    if (DataContext is not KaraokePlayer.Presentation.QueueViewModel viewModel)
    {
      return;
    }

    if (sender is not System.Windows.Controls.ListBox listBox)
    {
      return;
    }

    if (!e.Data.GetDataPresent(typeof(KaraokeCore.Library.SongEntry)))
    {
      return;
    }

    var droppedData = e.Data.GetData(typeof(KaraokeCore.Library.SongEntry)) as KaraokeCore.Library.SongEntry;
    if (droppedData is null)
    {
      return;
    }

    var targetItem = GetListBoxItemUnderMouse(listBox, e.GetPosition(listBox));
    var targetData = targetItem is null
      ? null
      : listBox.ItemContainerGenerator.ItemFromContainer(targetItem) as KaraokeCore.Library.SongEntry;

    if (targetData is null || ReferenceEquals(droppedData, targetData))
    {
      return;
    }

    var list = viewModel.Queue as ObservableCollection<KaraokeCore.Library.SongEntry>;
    if (list is null)
    {
      return;
    }

    var oldIndex = list.IndexOf(droppedData);
    var newIndex = list.IndexOf(targetData);
    if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
    {
      return;
    }

    list.Move(oldIndex, newIndex);
  }

  private static System.Windows.Controls.ListBoxItem? GetListBoxItemUnderMouse(System.Windows.Controls.ListBox listBox, System.Windows.Point position)
  {
    var element = listBox.InputHitTest(position) as DependencyObject;
    while (element is not null && element is not System.Windows.Controls.ListBoxItem)
    {
      element = VisualTreeHelper.GetParent(element);
    }

    return element as System.Windows.Controls.ListBoxItem;
  }

  private void QueueView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (_overlayWindow is not null)
    {
      _overlayWindow.DataContext = DataContext;
    }
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
    PreviewSurface.MediaPlayer = _videoPlayer;
    _audioPlayer.TimeChanged += AudioPlayer_TimeChanged;

    if (DataContext is KaraokePlayer.Presentation.QueueViewModel viewModel)
    {
      if (viewModel is System.ComponentModel.INotifyPropertyChanged notifier)
      {
        notifier.PropertyChanged += ViewModel_PropertyChanged;
      }
      PlaySelected(viewModel);
    }

    _hostWindow = Window.GetWindow(this);
    if (_overlayWindow is null)
    {
      _overlayWindow = new QueueOverlayWindow
      {
        Owner = _hostWindow,
        DataContext = DataContext
      };
      _overlayWindow.Loaded += OverlayWindow_Loaded;
      _overlayWindow.Show();
    }

    QueueViewbox.SizeChanged += OverlayHost_SizeChanged;
    PreviewSurface.SizeChanged += OverlayHost_SizeChanged;
    PreviewHost.SizeChanged += OverlayHost_SizeChanged;
    LayoutUpdated += OverlayHost_LayoutUpdated;
    if (_hostWindow is not null)
    {
      _hostWindow.LocationChanged += HostWindow_LocationChanged;
      _hostWindow.SizeChanged += HostWindow_SizeChanged;
    }
    Dispatcher.BeginInvoke(UpdateOverlayPlacement, DispatcherPriority.Loaded);
  }

  private void OnUnloaded(object sender, RoutedEventArgs e)
  {
    if (DataContext is System.ComponentModel.INotifyPropertyChanged notifier)
    {
      notifier.PropertyChanged -= ViewModel_PropertyChanged;
    }

    if (_overlayWindow is not null)
    {
      _overlayWindow.Loaded -= OverlayWindow_Loaded;
      _overlayWindow.Close();
      _overlayWindow = null;
    }

    QueueViewbox.SizeChanged -= OverlayHost_SizeChanged;
    PreviewSurface.SizeChanged -= OverlayHost_SizeChanged;
    PreviewHost.SizeChanged -= OverlayHost_SizeChanged;
    LayoutUpdated -= OverlayHost_LayoutUpdated;
    if (_hostWindow is not null)
    {
      _hostWindow.LocationChanged -= HostWindow_LocationChanged;
      _hostWindow.SizeChanged -= HostWindow_SizeChanged;
      _hostWindow = null;
    }

    PreviewSurface.MediaPlayer = null;
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

  private void OverlayHost_SizeChanged(object sender, SizeChangedEventArgs e)
  {
    UpdateOverlayPlacement();
  }

  private void OverlayHost_LayoutUpdated(object? sender, EventArgs e)
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

  private void OverlayWindow_Loaded(object? sender, RoutedEventArgs e)
  {
    UpdateOverlayPlacement();
  }

  private void UpdateOverlayPlacement()
  {
    if (!IsLoaded)
    {
      return;
    }

    if (_overlayWindow is null)
    {
      return;
    }

    var anchorElement = PreviewSurface.Visibility == Visibility.Visible ? (FrameworkElement)PreviewSurface : PreviewHost;
    var previewHeight = anchorElement.ActualHeight;
    var previewWidth = anchorElement.ActualWidth;
    if (previewHeight <= 0)
    {
      return;
    }
    if (previewWidth <= 0)
    {
      return;
    }

    var source = _hostWindow is null
      ? PresentationSource.FromVisual(PreviewHost)
      : PresentationSource.FromVisual(_hostWindow);
    if (source?.CompositionTarget is null)
    {
      return;
    }

    if (_hostWindow is null)
    {
      return;
    }

    if (!IsAncestorOf(_hostWindow, anchorElement))
    {
      return;
    }

    var bounds = anchorElement.TransformToAncestor(_hostWindow)
      .TransformBounds(new Rect(0, 0, previewWidth, previewHeight));
    var topLeftScreen = _hostWindow.PointToScreen(new System.Windows.Point(bounds.Left, bounds.Top));
    var bottomRightScreen = _hostWindow.PointToScreen(new System.Windows.Point(bounds.Right, bounds.Bottom));
    var transformFromDevice = source.CompositionTarget.TransformFromDevice;
    var topLeft = transformFromDevice.Transform(topLeftScreen);
    var bottomRight = transformFromDevice.Transform(bottomRightScreen);
    var renderedWidth = Math.Max(0, bottomRight.X - topLeft.X);
    var renderedHeight = Math.Max(0, bottomRight.Y - topLeft.Y);
    if (renderedWidth <= 0 || renderedHeight <= 0)
    {
      return;
    }

    var overlayBaseHeight = FindResource("QueueOverlayHeight") as double? ?? _defaultOverlayHeight;
    var scale = previewHeight <= 0 ? 1d : renderedHeight / previewHeight;
    if (scale <= 0)
    {
      scale = 1d;
    }

    var overlayHeight = overlayBaseHeight * scale;
    if (overlayHeight > renderedHeight)
    {
      overlayHeight = renderedHeight;
    }

    _overlayWindow.Width = renderedWidth;
    _overlayWindow.Height = renderedHeight;
    _overlayWindow.SetBarHeight(overlayHeight);
    _overlayWindow.Left = topLeft.X;
    _overlayWindow.Top = topLeft.Y;
  }

  private static bool IsAncestorOf(DependencyObject ancestor, DependencyObject descendant)
  {
    var current = descendant;
    while (current is not null)
    {
      if (ReferenceEquals(current, ancestor))
      {
        return true;
      }

      current = VisualTreeHelper.GetParent(current);
    }

    return false;
  }

  private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    const string selectedSongProperty = nameof(KaraokePlayer.Presentation.QueueViewModel.SelectedSong);
    if (e.PropertyName == selectedSongProperty && DataContext is KaraokePlayer.Presentation.QueueViewModel viewModel)
    {
      PlaySelected(viewModel);
    }
  }

  private void PlaySelected(KaraokePlayer.Presentation.QueueViewModel viewModel)
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
      _audioPlayer?.Stop();
      _videoPlayer?.Stop();
      PreviewSurface.MediaPlayer = null;
      PreviewSurface.Visibility = Visibility.Collapsed;
      return;
    }

    if (hasVideo)
    {
      PreviewSurface.MediaPlayer = _videoPlayer;
      PreviewSurface.Visibility = Visibility.Visible;
    }
    else
    {
      PreviewSurface.MediaPlayer = _videoPlayer;
      PreviewSurface.Visibility = Visibility.Hidden;
      _videoPlayer.Stop();
      _videoPlayer.Media = null;
    }

    using var audioMedia = new LibVLCSharp.Shared.Media(_libVlc, audioPath, LibVLCSharp.Shared.FromType.FromPath);
    _audioPlayer?.Play(audioMedia);

    if (hasVideo && !string.IsNullOrWhiteSpace(videoPath))
    {
      _videoPlayer?.Stop();
      _videoPlayer!.Media = null;
      using var videoMedia = new LibVLCSharp.Shared.Media(_libVlc, videoPath, LibVLCSharp.Shared.FromType.FromPath);
      _videoPlayer?.Play(videoMedia);
      SyncVideoToAudio(_audioPlayer?.Time ?? 0);
    }
    else
    {
      _videoPlayer?.Stop();
      if (_videoPlayer is not null)
      {
        _videoPlayer.Media = null;
      }
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
      PreviewSurface.Visibility = Visibility.Hidden;
      if (_videoPlayer.IsPlaying)
      {
        _videoPlayer.Pause();
      }
      _videoPlayer.Time = 0;
      return;
    }

    PreviewSurface.Visibility = Visibility.Visible;
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
}
