namespace KaraokeCore.Models;

public sealed class UltraStarSong
{
  public UltraStarSong(SongMetadata metadata, List<IUltraStarEvent> events)
  {
    Metadata = metadata;
    Events = events;
  }

  public SongMetadata Metadata { get; }
  public List<IUltraStarEvent> Events { get; }
}
