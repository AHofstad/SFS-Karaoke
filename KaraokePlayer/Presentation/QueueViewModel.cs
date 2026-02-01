using System.ComponentModel;
using System.Runtime.CompilerServices;
using KaraokeCore.Library;
using System.Collections.ObjectModel;

namespace KaraokePlayer.Presentation;

public sealed class QueueViewModel : INotifyPropertyChanged
{
  private const int PreviewWindowSize = 7;
  private readonly MainViewModel _root;
  private IReadOnlyList<PreviewSongItem> _previewItems = Array.Empty<PreviewSongItem>();
  private SongEntry? _selectedQueueItem;

  public QueueViewModel(MainViewModel root)
  {
    _root = root;
    _root.PropertyChanged += Root_PropertyChanged;
    BuildPreviewItems();
  }

  public IReadOnlyList<SongEntry> Library => _root.Songs;
  public SongEntry? SelectedSong
  {
    get => _root.SelectedSong;
    set
    {
      if (_root.SelectedSong == value)
      {
        return;
      }

      _root.SelectedSong = value;
      OnPropertyChanged();
      BuildPreviewItems();
    }
  }

  public string SelectedSongBackgroundPath => _root.SelectedSongBackgroundPath;

  public bool HasSelectedVideo => _root.HasSelectedVideo;

  public IReadOnlyList<SongEntry> Queue => _root.Queue;

  public SongEntry? SelectedQueueItem
  {
    get => _selectedQueueItem;
    set
    {
      if (_selectedQueueItem == value)
      {
        return;
      }

      _selectedQueueItem = value;
      OnPropertyChanged();
    }
  }

  public IReadOnlyList<PreviewSongItem> PreviewItems
  {
    get => _previewItems;
    private set
    {
      _previewItems = value;
      OnPropertyChanged();
    }
  }

  public event PropertyChangedEventHandler? PropertyChanged;

  private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  private void Root_PropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    const string selectedSongProperty = nameof(MainViewModel.SelectedSong);
    const string backgroundProperty = nameof(MainViewModel.SelectedSongBackgroundPath);
    const string hasVideoProperty = nameof(MainViewModel.HasSelectedVideo);
    if (e.PropertyName == selectedSongProperty)
    {
      OnPropertyChanged(nameof(SelectedSong));
      OnPropertyChanged(nameof(SelectedSongBackgroundPath));
      OnPropertyChanged(nameof(HasSelectedVideo));
      BuildPreviewItems();
      return;
    }

    if (e.PropertyName == backgroundProperty)
    {
      OnPropertyChanged(nameof(SelectedSongBackgroundPath));
      return;
    }

    if (e.PropertyName == hasVideoProperty)
    {
      OnPropertyChanged(nameof(HasSelectedVideo));
    }
  }

  public bool RemoveFromQueue(SongEntry? entry)
  {
    return _root.RemoveFromQueue(entry);
  }

  public bool MoveQueueItemUp(SongEntry? entry)
  {
    return _root.MoveQueueItemUp(entry);
  }

  public bool MoveQueueItemDown(SongEntry? entry)
  {
    return _root.MoveQueueItemDown(entry);
  }

  private void BuildPreviewItems()
  {
    if (Library.Count == 0)
    {
      PreviewItems = Array.Empty<PreviewSongItem>();
      return;
    }

    var selected = SelectedSong ?? Library[0];
    var selectedIndex = 0;
    for (var i = 0; i < Library.Count; i++)
    {
      if (ReferenceEquals(Library[i], selected))
      {
        selectedIndex = i;
        break;
      }
    }
    if (selectedIndex < 0)
    {
      selectedIndex = 0;
    }

    var windowSize = Math.Min(PreviewWindowSize, Library.Count);
    if (windowSize % 2 == 0)
    {
      windowSize -= 1;
    }
    if (windowSize <= 0)
    {
      PreviewItems = Array.Empty<PreviewSongItem>();
      return;
    }
    var half = windowSize / 2;
    var items = new List<PreviewSongItem>(windowSize);

    for (var offset = -half; offset <= half; offset++)
    {
      var index = WrapIndex(selectedIndex + offset, Library.Count);
      var song = Library[index];
      items.Add(new PreviewSongItem(song, index == selectedIndex));
    }

    PreviewItems = items;
  }

  private static int WrapIndex(int index, int count)
  {
    if (count == 0)
    {
      return 0;
    }

    var result = index % count;
    return result < 0 ? result + count : result;
  }
}
