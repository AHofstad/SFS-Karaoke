using KaraokeCore.Library;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;

namespace KaraokeCore.Tests.Library;

public class SongLibraryScannerTests
{
  [Test]
  public void Scan_SongFolders_ResolvesMedia()
  {
    // arrange
    const string root = @"c:\songs";
    const string songA = @"c:\songs\SongA";
    const string songB = @"c:\songs\SongB";
    const int expectedCount = 2;

    var txtA = """
    #TITLE:Song A
    #ARTIST:Artist A
    #MP3:track-a.mp3
    #VIDEO:video-a.mp4
    #COVER:cover-a.png
    #BACKGROUND:bg-a.jpg
    #BPM:120
    #GAP:1000
    : 0 4 0 la
    E
    """;

    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songA}\song.txt", new MockFileData(txtA) },
      { $@"{songA}\track-a.mp3", new MockFileData(string.Empty) },
      { $@"{songA}\video-a.mp4", new MockFileData(string.Empty) },
      { $@"{songA}\cover-a.png", new MockFileData(string.Empty) },
      { $@"{songA}\bg-a.jpg", new MockFileData(string.Empty) },
      { $@"{songB}\anything.txt", new MockFileData("#TITLE:Song B\n#BPM:90\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{songB}\track-b.mp3", new MockFileData(string.Empty) },
      { $@"{songB}\video-b.mp4", new MockFileData(string.Empty) },
    });

    // act
    var scanner = new SongLibraryScanner(fileSystemMock);
    var entries = scanner.Scan(root);

    // assert
    Assert.That(entries.Count, Is.EqualTo(expectedCount));

    var entryA = entries.First(e => e.Metadata?.Title == "Song A");
    const string songATrack = "track-a.mp3";
    const string songAVideo = "video-a.mp4";
    const string songACover = "cover-a.png";
    const string songABackground = "bg-a.jpg";
    Assert.That(entryA.AudioPath, Does.EndWith(songATrack));
    Assert.That(entryA.VideoPath, Does.EndWith(songAVideo));
    Assert.That(entryA.CoverPath, Does.EndWith(songACover));
    Assert.That(entryA.BackgroundPath, Does.EndWith(songABackground));

    var entryB = entries.First(e => e.Metadata?.Title == "Song B");
    const string songBTrack = "track-b.mp3";
    const string songBVideo = "video-b.mp4";
    Assert.That(entryB.AudioPath, Does.EndWith(songBTrack));
    Assert.That(entryB.VideoPath, Does.EndWith(songBVideo));
  }

  [Test]
  public void Scan_RootMissing_ReturnsEmptyList()
  {
    // arrange
    const string missingRoot = @"c:\missing";
    var fileSystemMock = new MockFileSystem();

    // act
    var scanner = new SongLibraryScanner(fileSystemMock);
    var entries = scanner.Scan(missingRoot);

    // assert
    Assert.That(entries, Is.Empty);
  }

  [Test]
  public void Scan_MetadataMissing_UsesFirstMatchingMedia()
  {
    // Arrange
    const string root = @"c:\songs";
    const string song = @"c:\songs\Song";
    const int expectedCount = 1;
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{song}\song.txt", new MockFileData("#TITLE:No Media Tags\n#BPM:100\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{song}\a-first.mp3", new MockFileData(string.Empty) },
      { $@"{song}\b-second.mp3", new MockFileData(string.Empty) },
    });

    // Act
    var scanner = new SongLibraryScanner(fileSystemMock);
    var entries = scanner.Scan(root);

    // Assert
    Assert.That(entries.Count, Is.EqualTo(expectedCount));
    const int firstEntryIndex = 0;
    const string firstAudio = "a-first.mp3";
    Assert.That(entries[firstEntryIndex].AudioPath, Does.EndWith(firstAudio));
  }

  [Test]
  public void Scan_NestedFolders_FindsSongFoldersRecursively()
  {
    // Arrange
    const string root = @"c:\songs";
    const string songA = @"c:\songs\ArtistA\SongA";
    const string songB = @"c:\songs\SongB";
    const int expectedCount = 2;

    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songA}\song.txt", new MockFileData("#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{songA}\track-a.mp3", new MockFileData(string.Empty) },
      { $@"{songB}\song.txt", new MockFileData("#TITLE:Song B\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{songB}\track-b.mp3", new MockFileData(string.Empty) },
    });

    // Act
    var scanner = new SongLibraryScanner(fileSystemMock);
    var entries = scanner.Scan(root);

    // Assert
    Assert.That(entries.Count, Is.EqualTo(expectedCount));
    Assert.That(entries.Any(entry => entry.Metadata?.Title == "Song A"), Is.True);
    Assert.That(entries.Any(entry => entry.Metadata?.Title == "Song B"), Is.True);
  }

  [Test]
  public void Scan_SongFolderWithSubfolder_IgnoresNestedSongs()
  {
    // Arrange
    const string root = @"c:\songs";
    const string songFolder = @"c:\songs\SongA";
    const string nestedSong = @"c:\songs\SongA\NestedSong";
    const int expectedCount = 1;

    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{songFolder}\song.txt", new MockFileData("#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{songFolder}\track-a.mp3", new MockFileData(string.Empty) },
      { $@"{nestedSong}\song.txt", new MockFileData("#TITLE:Nested\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{nestedSong}\track-n.mp3", new MockFileData(string.Empty) },
    });

    // Act
    var scanner = new SongLibraryScanner(fileSystemMock);
    var entries = scanner.Scan(root);

    // Assert
    Assert.That(entries.Count, Is.EqualTo(expectedCount));
    Assert.That(entries.Any(entry => entry.Metadata?.Title == "Song A"), Is.True);
    Assert.That(entries.Any(entry => entry.Metadata?.Title == "Nested"), Is.False);
  }
}
