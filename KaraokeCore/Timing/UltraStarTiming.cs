using KaraokeCore.Models;

namespace KaraokeCore.Timing;

public sealed class UltraStarTiming
{
  private const double _msPerMinute = 60000.0;
  private const double _beatMultiplier = 4.0;
  private const double _minBpm = 0.0;
  private const int _defaultGapMs = 0;
  private const int _noNoteBeat = -1;

  public UltraStarTiming(double bpm, int gapMs)
  {
    if (double.IsNaN(bpm) || double.IsInfinity(bpm) || bpm <= _minBpm)
    {
      throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be a positive finite value.");
    }

    Bpm = bpm;
    GapMs = gapMs;
  }

  public double Bpm { get; }
  public int GapMs { get; }

  public double BeatDurationMs => _msPerMinute / (Bpm * _beatMultiplier);

  public double BeatsToMs(int beats)
  {
    return beats * BeatDurationMs;
  }

  public double BeatToMs(int beat)
  {
    return GapMs + BeatsToMs(beat);
  }

  public double NoteStartMs(NoteEvent note)
  {
    return BeatToMs(note.StartBeat);
  }

  public double NoteDurationMs(NoteEvent note)
  {
    return BeatsToMs(note.Length);
  }

  public double? FirstNoteStartMs(IReadOnlyList<IUltraStarEvent> events)
  {
    var firstBeat = FirstNoteBeat(events);
    if (firstBeat == _noNoteBeat)
    {
      return null;
    }

    return BeatToMs(firstBeat);
  }

  public static int FirstNoteBeat(IReadOnlyList<IUltraStarEvent> events)
  {
    var firstBeat = _noNoteBeat;

    foreach (var evt in events)
    {
      if (evt is not NoteEvent note)
      {
        continue;
      }

      if (firstBeat == _noNoteBeat || note.StartBeat < firstBeat)
      {
        firstBeat = note.StartBeat;
      }
    }

    return firstBeat;
  }

  public static UltraStarTiming? TryCreate(SongMetadata metadata)
  {
    if (metadata.Bpm is null)
    {
      return null;
    }

    var gap = metadata.GapMs ?? _defaultGapMs;
    if (metadata.Bpm <= _minBpm)
    {
      return null;
    }

    return new UltraStarTiming(metadata.Bpm.Value, gap);
  }
}
