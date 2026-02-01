namespace KaraokeCore.Models;

public enum NoteType
{
  Normal,
  Golden,
  Freestyle,
  Rap,
  RapGolden,
}

public interface IUltraStarEvent
{
}

public sealed record NoteEvent(
  NoteType Type,
  int StartBeat,
  int Length,
  int Pitch,
  string Text
) : IUltraStarEvent;

public sealed record PhraseEndEvent(int StartBeat) : IUltraStarEvent;

public sealed record PlayerMarkerEvent(string Player) : IUltraStarEvent;
