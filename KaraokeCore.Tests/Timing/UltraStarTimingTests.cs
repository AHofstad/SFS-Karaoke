using KaraokeCore.Models;
using KaraokeCore.Timing;
using NUnit.Framework;

namespace KaraokeCore.Tests.Timing;

public class UltraStarTimingTests
{
  [Test]
  public void BeatDuration_UsesBpmTimesFour_ExpectedMilliseconds()
  {
    // Arrange
    const double bpm = 120.0;
    const int gapMs = 0;
    const double expectedDurationMs = 125.0;
    var timing = new UltraStarTiming(bpm, gapMs);

    // Act
    var duration = timing.BeatDurationMs;

    // Assert
    Assert.That(duration, Is.EqualTo(expectedDurationMs));
  }

  [Test]
  public void BeatToMs_WithGap_IncludesGapOffset()
  {
    // Arrange
    const double bpm = 120.0;
    const int gapMs = 1000;
    const int beat = 8;
    const double expectedMs = 2000.0;
    var timing = new UltraStarTiming(bpm, gapMs);

    // Act
    var ms = timing.BeatToMs(beat);

    // Assert
    Assert.That(ms, Is.EqualTo(expectedMs));
  }

  [Test]
  public void NoteTiming_StartAndLength_ExpectedMilliseconds()
  {
    // Arrange
    const double bpm = 100.0;
    const int gapMs = 500;
    const int startBeat = 4;
    const int length = 8;
    const int pitch = 0;
    const string lyric = "la";
    const double expectedStartMs = 1100.0;
    const double expectedDurationMs = 1200.0;
    var timing = new UltraStarTiming(bpm, gapMs);
    var note = new NoteEvent(NoteType.Normal, startBeat, length, pitch, lyric);

    // Act
    var startMs = timing.NoteStartMs(note);
    var durationMs = timing.NoteDurationMs(note);

    // Assert
    Assert.That(startMs, Is.EqualTo(expectedStartMs));
    Assert.That(durationMs, Is.EqualTo(expectedDurationMs));
  }

  [Test]
  public void TryCreate_NoBpm_ReturnsNull()
  {
    // Arrange
    var metadata = new SongMetadata(new Dictionary<string, string>());

    // Act
    var timing = UltraStarTiming.TryCreate(metadata);

    // Assert
    Assert.That(timing, Is.Null);
  }

  [Test]
  public void FirstNoteBeat_EventsContainNotes_ReturnsEarliestBeat()
  {
    // Arrange
    const int firstBeat = 4;
    const int laterBeat = 12;
    var events = new List<IUltraStarEvent>
    {
      new PhraseEndEvent(2),
      new NoteEvent(NoteType.Normal, laterBeat, 2, 0, "la"),
      new PlayerMarkerEvent("P1"),
      new NoteEvent(NoteType.Golden, firstBeat, 3, 0, "ta"),
    };

    // Act
    var beat = UltraStarTiming.FirstNoteBeat(events);

    // Assert
    Assert.That(beat, Is.EqualTo(firstBeat));
  }

  [Test]
  public void FirstNoteStartMs_EventsContainNotes_ReturnsTime()
  {
    // Arrange
    const double bpm = 120.0;
    const int gapMs = 1000;
    const int firstBeat = 8;
    const double expectedStartMs = 2000.0;
    var timing = new UltraStarTiming(bpm, gapMs);
    var events = new List<IUltraStarEvent>
    {
      new NoteEvent(NoteType.Normal, firstBeat, 2, 0, "la"),
    };

    // Act
    var startMs = timing.FirstNoteStartMs(events);

    // Assert
    Assert.That(startMs, Is.EqualTo(expectedStartMs));
  }

  [Test]
  public void FirstNoteStartMs_NoNotes_ReturnsNull()
  {
    // Arrange
    const double bpm = 120.0;
    const int gapMs = 0;
    var timing = new UltraStarTiming(bpm, gapMs);
    var events = new List<IUltraStarEvent>
    {
      new PhraseEndEvent(2),
      new PlayerMarkerEvent("P2"),
    };

    // Act
    var startMs = timing.FirstNoteStartMs(events);

    // Assert
    Assert.That(startMs, Is.Null);
  }
}
