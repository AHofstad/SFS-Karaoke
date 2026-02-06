using System.Text;
using KaraokeCore.Models;
using KaraokeCore.Timing;

namespace KaraokePlayer.Presentation;

public static class LyricLineBuilder
{
  public static IReadOnlyList<LyricLine> BuildLines(UltraStarSong song)
  {
    var timing = UltraStarTiming.TryCreate(song.Metadata);
    if (timing is null)
    {
      return Array.Empty<LyricLine>();
    }

    var lines = new List<LyricLine>();
    var notes = new List<NoteEvent>();

    foreach (var evt in song.Events)
    {
      if (evt is NoteEvent note)
      {
        notes.Add(note);
        continue;
      }

      if (evt is PhraseEndEvent phraseEnd)
      {
        AddLine(notes, timing, phraseEnd.StartBeat, lines);
        notes.Clear();
      }
    }

    if (notes.Count > 0)
    {
      var lastNote = notes[^1];
      var endBeat = lastNote.StartBeat + lastNote.Length;
      AddLine(notes, timing, endBeat, lines);
    }

    return lines;
  }

  private static void AddLine(List<NoteEvent> notes, UltraStarTiming timing, int endBeat, List<LyricLine> lines)
  {
    if (notes.Count == 0)
    {
      return;
    }

    var startBeat = notes.Min(note => note.StartBeat);
    var startMs = timing.BeatToMs(startBeat);
    var endMs = timing.BeatToMs(endBeat);
    if (endMs < startMs)
    {
      endMs = startMs;
    }

    var tokens = BuildTokens(notes, timing);
    var text = string.Join(' ', tokens.Select(token => token.Text)).Trim();
    lines.Add(new LyricLine(startMs, endMs, text, tokens));
  }

  private static IReadOnlyList<LyricToken> BuildTokens(IEnumerable<NoteEvent> notes, UltraStarTiming timing)
  {
    var parts = new List<LyricToken>();
    foreach (var note in notes)
    {
      var text = NormalizeText(note.Text);
      if (!string.IsNullOrWhiteSpace(text))
      {
        var startMs = timing.NoteStartMs(note);
        var endMs = startMs + timing.NoteDurationMs(note);
        parts.Add(new LyricToken(text.Trim(), startMs, endMs));
      }
    }

    return parts;
  }

  private static string NormalizeText(string text)
  {
    if (string.IsNullOrEmpty(text))
    {
      return string.Empty;
    }

    if (text == "~")
    {
      return string.Empty;
    }

    return text
      .Replace('\u2019', '\'')
      .Replace('\u2018', '\'');
  }
}
