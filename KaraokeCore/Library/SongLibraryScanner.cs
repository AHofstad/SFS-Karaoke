using KaraokeCore.Models;
using KaraokeCore.Parsing;
using System.IO.Abstractions;

namespace KaraokeCore.Library;

public sealed class SongLibraryScanner
{
  private static readonly string[] AudioExtensions = [".mp3", ".ogg", ".wav", ".flac"];
  private static readonly string[] VideoExtensions = [".mp4", ".mkv", ".avi", ".webm"];
  private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp"];

  private readonly IFileSystem _fileSystem;

  public SongLibraryScanner()
    : this(new FileSystem())
  {
  }

  public SongLibraryScanner(IFileSystem fileSystem)
  {
    _fileSystem = fileSystem;
  }

  public IReadOnlyList<SongEntry> Scan(string rootFolder, Action<int>? onSongImported = null)
  {
    if (string.IsNullOrWhiteSpace(rootFolder))
    {
      throw new ArgumentException("Root folder is required.", nameof(rootFolder));
    }

    if (!_fileSystem.Directory.Exists(rootFolder))
    {
      return Array.Empty<SongEntry>();
    }

    var entries = new List<SongEntry>();
    var loaded = 0;
    foreach (var (folder, txtPath) in EnumerateSongFolders(rootFolder))
    {
      entries.Add(ScanSongFolder(folder, txtPath));
      loaded++;
      onSongImported?.Invoke(loaded);
    }

    return entries;
  }

  private IEnumerable<(string Folder, string TxtPath)> EnumerateSongFolders(string rootFolder)
  {
    var folders = new Queue<string>(_fileSystem.Directory.EnumerateDirectories(rootFolder));

    while (folders.Count > 0)
    {
      var folder = folders.Dequeue();
      var txtPath = _fileSystem.Directory.EnumerateFiles(folder, "*.txt").FirstOrDefault();
      if (txtPath is not null)
      {
        yield return (folder, txtPath);
        continue;
      }

      foreach (var subfolder in _fileSystem.Directory.EnumerateDirectories(folder))
      {
        folders.Enqueue(subfolder);
      }
    }
  }

  private SongEntry ScanSongFolder(string folder, string? txtPath = null)
  {
    txtPath ??= _fileSystem.Directory.EnumerateFiles(folder, "*.txt").FirstOrDefault();
    SongMetadata? metadata = null;

    if (txtPath != null)
    {
      var parser = new UltraStarParser();
      var lines = UltraStarTextLoader.ReadAllLines(_fileSystem, txtPath);
      metadata = parser.Parse(lines).Metadata;
    }

    var audioPath = ResolveMediaPath(folder, metadata?.Audio, AudioExtensions);
    var videoPath = ResolveMediaPath(folder, metadata?.Video, VideoExtensions);
    var coverPath = ResolveMediaPath(folder, metadata?.Cover, ImageExtensions);
    var backgroundPath = ResolveMediaPath(folder, metadata?.Background, ImageExtensions);

    return new SongEntry(folder, txtPath, metadata, audioPath, videoPath, coverPath, backgroundPath);
  }

  private string? ResolveMediaPath(string folder, string? candidate, string[] extensions)
  {
    if (!string.IsNullOrWhiteSpace(candidate))
    {
      var fromMetadata = _fileSystem.Path.Combine(folder, candidate);
      if (_fileSystem.File.Exists(fromMetadata))
      {
        return fromMetadata;
      }
    }

    return _fileSystem.Directory.EnumerateFiles(folder)
      .FirstOrDefault(path => extensions.Contains(_fileSystem.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));
  }
}
