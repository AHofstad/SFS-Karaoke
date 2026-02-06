using System.IO.Abstractions;
using KaraokeCore.Models;
using KaraokeCore.Parsing;

namespace KaraokeCore.Library;

public sealed class SongLibraryScanner
{
  private static readonly string[] _audioExtensions = [".mp3", ".ogg", ".wav", ".flac"];
  private static readonly string[] _videoExtensions = [".mp4", ".mkv", ".avi", ".webm"];
  private static readonly string[] _imageExtensions = [".jpg", ".jpeg", ".png", ".bmp"];

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
      var entry = ScanSongFolder(folder, txtPath);
      if (string.IsNullOrWhiteSpace(entry.AudioPath))
      {
        continue;
      }

      entries.Add(entry);
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

    var audioPath = ResolveMediaPath(folder, metadata?.Audio, _audioExtensions);
    var videoPath = ResolveMediaPath(folder, metadata?.Video, _videoExtensions);
    var coverPath = ResolveMediaPath(folder, metadata?.Cover, _imageExtensions);
    var backgroundPath = ResolveMediaPath(folder, metadata?.Background, _imageExtensions);

    return new SongEntry(folder, txtPath, metadata, audioPath, videoPath, coverPath, backgroundPath);
  }

  private string? ResolveMediaPath(string folder, string? candidate, string[] extensions)
  {
    if (!string.IsNullOrWhiteSpace(candidate))
    {
      var fromMetadata = _fileSystem.Path.Combine(folder, candidate);
      if (_fileSystem.File.Exists(fromMetadata)
          && extensions.Contains(_fileSystem.Path.GetExtension(fromMetadata), StringComparer.OrdinalIgnoreCase))
      {
        return fromMetadata;
      }
    }

    return _fileSystem.Directory.EnumerateFiles(folder)
      .FirstOrDefault(path => extensions.Contains(_fileSystem.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));
  }
}
