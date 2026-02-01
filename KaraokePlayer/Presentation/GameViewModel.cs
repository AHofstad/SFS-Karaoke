using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.IO.Abstractions;
using KaraokeCore.Library;
using KaraokeCore.Parsing;
using KaraokeCore.Timing;

namespace KaraokePlayer.Presentation;

public sealed class GameViewModel : INotifyPropertyChanged
{
  private const double LyricLeadInMs = 300;
  private const double ActiveTokenToleranceMs = 50;
  private const double HighlightOffsetMs = 0;
  private const int ProgressMax = 100;
  private const string DefaultElapsedTime = "00:00";
  private readonly MainViewModel _root;
  private double _remainingProgress;
  private string _elapsedTime = DefaultElapsedTime;
  private string _skipPromptText = KaraokePlayer.Resources.Strings.SkipToFirstNotePrompt;
  private bool _isSkipPromptVisible;
  private string _currentLyricLineText = string.Empty;
  private string _nextLyricLineText = string.Empty;
  private IReadOnlyList<LyricTokenView> _currentLyricTokens = Array.Empty<LyricTokenView>();
  private double? _firstNoteStartMs;
  private List<LyricLine> _lyrics = new();
  private int _currentLineIndex = -1;

  public GameViewModel(MainViewModel root)
  {
    _root = root;
  }

  public SongEntry? CurrentSong => _root.CurrentQueueSong;
  public string SongTitle => _root.CurrentQueueSong?.Metadata?.Title ?? "--";
  public string SongArtist => _root.CurrentQueueSong?.Metadata?.Artist ?? "--";
  public string BackgroundPath => _root.CurrentQueueSong?.BackgroundPath ?? string.Empty;
  public bool HasVideo => !string.IsNullOrWhiteSpace(_root.CurrentQueueSong?.VideoPath);
  public bool HasBackground => !string.IsNullOrWhiteSpace(BackgroundPath);

  public double RemainingProgress
  {
    get => _remainingProgress;
    set => SetField(ref _remainingProgress, value);
  }

  public string ElapsedTime
  {
    get => _elapsedTime;
    private set => SetField(ref _elapsedTime, value);
  }

  public string SkipPromptText
  {
    get => _skipPromptText;
    private set => SetField(ref _skipPromptText, value);
  }

  public bool IsSkipPromptVisible
  {
    get => _isSkipPromptVisible;
    private set => SetField(ref _isSkipPromptVisible, value);
  }

  public string CurrentLyricLineText
  {
    get => _currentLyricLineText;
    private set => SetField(ref _currentLyricLineText, value);
  }

  public string NextLyricLineText
  {
    get => _nextLyricLineText;
    private set => SetField(ref _nextLyricLineText, value);
  }

  public IReadOnlyList<LyricTokenView> CurrentLyricTokens
  {
    get => _currentLyricTokens;
    private set => SetField(ref _currentLyricTokens, value);
  }

  public double? FirstNoteStartMs
  {
    get => _firstNoteStartMs;
    private set => SetField(ref _firstNoteStartMs, value);
  }

  public event PropertyChangedEventHandler? PropertyChanged;
  public event EventHandler? SkipToFirstNoteRequested;

  public bool TryAdvanceQueue()
  {
    if (_root.PlayNextQueueSong())
    {
      OnCurrentSongChanged();
      return true;
    }

    _root.ShowQueue();
    return false;
  }

  public void OnCurrentSongChanged()
  {
    OnPropertyChanged(nameof(CurrentSong));
    OnPropertyChanged(nameof(SongTitle));
    OnPropertyChanged(nameof(SongArtist));
    OnPropertyChanged(nameof(BackgroundPath));
    OnPropertyChanged(nameof(HasVideo));
    OnPropertyChanged(nameof(HasBackground));
  }

  public void PrepareLyrics(SongEntry? song, IFileSystem fileSystem)
  {
    if (song?.TxtPath is null || !fileSystem.File.Exists(song.TxtPath))
    {
      ResetLyrics();
      FirstNoteStartMs = null;
      return;
    }

    var parser = new UltraStarParser();
    var lines = fileSystem.File.ReadAllLines(song.TxtPath);
    LoadLyrics(parser.Parse(lines));
  }

  public void UpdateLyricDisplay(double currentMs)
  {
    if (_lyrics.Count == 0)
    {
      CurrentLyricLineText = string.Empty;
      NextLyricLineText = string.Empty;
      CurrentLyricTokens = Array.Empty<LyricTokenView>();
      _currentLineIndex = -1;
      return;
    }

    if (currentMs < _lyrics[0].StartMs - LyricLeadInMs)
    {
      _currentLineIndex = -1;
      CurrentLyricLineText = string.Empty;
      NextLyricLineText = string.Empty;
      CurrentLyricTokens = Array.Empty<LyricTokenView>();
      return;
    }

    if (currentMs < _lyrics[0].StartMs)
    {
      _currentLineIndex = -1;
      CurrentLyricLineText = _lyrics[0].Text;
      NextLyricLineText = _lyrics.Count > 1 ? _lyrics[1].Text : string.Empty;
      CurrentLyricTokens = CreateTokenViews(_lyrics[0], currentMs);
      return;
    }

    var index = _currentLineIndex;
    if (index < 0)
    {
      index = 0;
    }

    while (index < _lyrics.Count && currentMs >= _lyrics[index].EndMs)
    {
      index++;
    }

    if (index >= _lyrics.Count)
    {
      _currentLineIndex = _lyrics.Count;
      CurrentLyricLineText = string.Empty;
      NextLyricLineText = string.Empty;
      CurrentLyricTokens = Array.Empty<LyricTokenView>();
      return;
    }

    _currentLineIndex = index;
    CurrentLyricLineText = _lyrics[index].Text;
    NextLyricLineText = index + 1 < _lyrics.Count ? _lyrics[index + 1].Text : string.Empty;
    CurrentLyricTokens = CreateTokenViews(_lyrics[index], currentMs);
  }

  public void UpdatePlaybackProgress(double currentMs, long totalMs)
  {
    if (totalMs <= 0 || currentMs < 0)
    {
      RemainingProgress = 0;
      ElapsedTime = DefaultElapsedTime;
      IsSkipPromptVisible = false;
      return;
    }

    var clampedMs = Math.Min(currentMs, totalMs);
    var percent = clampedMs / totalMs * ProgressMax;
    RemainingProgress = percent;
    ElapsedTime = FormatElapsedTime(clampedMs);
    SkipPromptText = KaraokePlayer.Resources.Strings.SkipToFirstNotePrompt;
    UpdateSkipPromptVisibility(clampedMs);
  }

  public void EnsureLyricsLoaded()
  {
    if (_lyrics.Count > 0)
    {
      return;
    }

    var path = CurrentSong?.TxtPath;
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    {
      return;
    }

    var parser = new UltraStarParser();
    var lines = File.ReadAllLines(path);
    LoadLyrics(parser.Parse(lines));
  }

  public void RequestSkipToFirstNote()
  {
    SkipToFirstNoteRequested?.Invoke(this, EventArgs.Empty);
  }

  private void LoadLyrics(KaraokeCore.Models.UltraStarSong song)
  {
    ResetLyrics();
    _lyrics = LyricLineBuilder.BuildLines(song).ToList();
    var timing = UltraStarTiming.TryCreate(song.Metadata);
    FirstNoteStartMs = timing?.FirstNoteStartMs(song.Events);
    if (_lyrics.Count > 0)
    {
      NextLyricLineText = _lyrics[0].Text;
    }
  }

  private void ResetLyrics()
  {
    _lyrics = new List<LyricLine>();
    _currentLineIndex = -1;
    CurrentLyricLineText = string.Empty;
    NextLyricLineText = string.Empty;
    CurrentLyricTokens = Array.Empty<LyricTokenView>();
    FirstNoteStartMs = null;
    IsSkipPromptVisible = false;
  }

  private static IReadOnlyList<LyricTokenView> CreateTokenViews(LyricLine line, double currentMs)
  {
    if (line.Tokens.Count == 0)
    {
      return Array.Empty<LyricTokenView>();
    }

    var activeIndex = FindActiveTokenIndex(line.Tokens, currentMs + HighlightOffsetMs);
    var tokens = new List<LyricTokenView>(line.Tokens.Count);
    for (var i = 0; i < line.Tokens.Count; i++)
    {
      var token = line.Tokens[i];
      var isActive = i == activeIndex;
      tokens.Add(new LyricTokenView($"{token.Text} ", isActive));
    }

    return tokens;
  }

  private static int FindActiveTokenIndex(IReadOnlyList<LyricToken> tokens, double currentMs)
  {
    if (tokens.Count == 0)
    {
      return -1;
    }

    if (currentMs < tokens[0].StartMs - ActiveTokenToleranceMs)
    {
      return -1;
    }

    for (var i = 0; i < tokens.Count; i++)
    {
      var token = tokens[i];
      var start = token.StartMs - ActiveTokenToleranceMs;
      var end = token.EndMs + ActiveTokenToleranceMs;
      if (currentMs >= start && currentMs < end)
      {
        return i;
      }
    }

    for (var i = 1; i < tokens.Count; i++)
    {
      if (currentMs < tokens[i].StartMs - ActiveTokenToleranceMs)
      {
        return i - 1;
      }
    }

    return tokens.Count - 1;
  }

  private static string FormatElapsedTime(double currentMs)
  {
    var time = TimeSpan.FromMilliseconds(Math.Max(0, currentMs));
    var minutes = (int)time.TotalMinutes;
    return $"{minutes:00}:{time.Seconds:00}";
  }

  private void UpdateSkipPromptVisibility(double currentMs)
  {
    if (FirstNoteStartMs is null)
    {
      IsSkipPromptVisible = false;
      return;
    }

    var leadMs = FirstNoteStartMs.Value - currentMs;
    IsSkipPromptVisible = leadMs > 3000;
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
