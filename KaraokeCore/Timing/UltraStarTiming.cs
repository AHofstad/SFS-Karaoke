using KaraokeCore.Models;

namespace KaraokeCore.Timing;

public sealed class UltraStarTiming
{
  private const double MsPerMinute = 60000.0;
  private const double BeatMultiplier = 4.0;
  private const double MinBpm = 0.0;
  private const int DefaultGapMs = 0;
  private const int NoNoteBeat = -1;

  public UltraStarTiming(double bpm, int gapMs)
  {
    if (double.IsNaN(bpm) || double.IsInfinity(bpm) || bpm <= MinBpm)
    {
      throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be a positive finite value.");
    }

    Bpm = bpm;
    GapMs = gapMs;
  }

  public double Bpm { get; }
  public int GapMs { get; }

  public double BeatDurationMs => MsPerMinute / (Bpm * BeatMultiplier);

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
    if (firstBeat == NoNoteBeat)
    {
      return null;
    }

    return BeatToMs(firstBeat);
  }

  public static int FirstNoteBeat(IReadOnlyList<IUltraStarEvent> events)
  {
    var firstBeat = NoNoteBeat;

    foreach (var evt in events)
    {
      if (evt is not NoteEvent note)
      {
        continue;
      }

      if (firstBeat == NoNoteBeat || note.StartBeat < firstBeat)
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

    var gap = metadata.GapMs ?? DefaultGapMs;
    if (metadata.Bpm <= MinBpm)
    {
      return null;
    }

    return new UltraStarTiming(metadata.Bpm.Value, gap);
  }
}
