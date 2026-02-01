using System.IO;
using KaraokeCore.Library;

namespace KaraokeCore.Queue;

public static class QueueSongId
{
  public static string FromEntry(string libraryPath, SongEntry entry)
  {
    var relative = Path.GetRelativePath(libraryPath, entry.FolderPath);
    return Normalize(relative);
  }

  public static string Normalize(string id)
  {
    return id.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
  }
}
