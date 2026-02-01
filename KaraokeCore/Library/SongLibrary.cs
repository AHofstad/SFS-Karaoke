using System.IO.Abstractions;

namespace KaraokeCore.Library;

public sealed class SongLibrary
{
  private readonly SongLibraryScanner _scanner;

  private SongLibrary(string rootPath, SongLibraryScanner scanner, IReadOnlyList<SongEntry> entries)
  {
    RootPath = rootPath;
    _scanner = scanner;
    Entries = entries;
  }

  public string RootPath { get; }
  public IReadOnlyList<SongEntry> Entries { get; private set; }

  public static SongLibrary Load(string rootPath, IFileSystem? fileSystem = null)
  {
    var scanner = new SongLibraryScanner(fileSystem ?? new FileSystem());
    var entries = scanner.Scan(rootPath);
    return new SongLibrary(rootPath, scanner, entries);
  }

  public void Refresh()
  {
    Entries = _scanner.Scan(RootPath);
  }
}
