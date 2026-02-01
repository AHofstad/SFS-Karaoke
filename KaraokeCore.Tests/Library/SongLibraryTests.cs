using KaraokeCore.Library;
using NUnit.Framework;
using System.IO.Abstractions.TestingHelpers;

namespace KaraokeCore.Tests.Library;

public class SongLibraryTests
{
  [Test]
  public void Load_ValidRootPath_ProvidesEntries()
  {
    // Arrange
    const string root = @"c:\songs";
    const int expectedCount = 2;
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{root}\SongA\song.txt", new MockFileData("#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{root}\SongA\track.mp3", new MockFileData(string.Empty) },
      { $@"{root}\SongB\song.txt", new MockFileData("#TITLE:Song B\n#BPM:90\n#GAP:0\n: 0 1 0 yo\nE") },
      { $@"{root}\SongB\track.mp3", new MockFileData(string.Empty) },
    });

    // Act
    var library = SongLibrary.Load(root, fileSystemMock);

    // Assert
    Assert.That(library.RootPath, Is.EqualTo(root));
    Assert.That(library.Entries.Count, Is.EqualTo(expectedCount));
  }

  [Test]
  public void Refresh_AfterChanges_UpdatesEntries()
  {
    // Arrange
    const string root = @"c:\songs";
    const int initialCount = 1;
    const int updatedCount = 2;
    var fileSystemMock = new MockFileSystem(new Dictionary<string, MockFileData>
    {
      { $@"{root}\SongA\song.txt", new MockFileData("#TITLE:Song A\n#BPM:120\n#GAP:0\n: 0 1 0 hi\nE") },
      { $@"{root}\SongA\track.mp3", new MockFileData(string.Empty) },
    });

    var library = SongLibrary.Load(root, fileSystemMock);
    Assert.That(library.Entries.Count, Is.EqualTo(initialCount));

    fileSystemMock.AddFile($@"{root}\SongB\song.txt", new MockFileData("#TITLE:Song B\n#BPM:90\n#GAP:0\n: 0 1 0 yo\nE"));
    fileSystemMock.AddFile($@"{root}\SongB\track.mp3", new MockFileData(string.Empty));

    // Act
    library.Refresh();

    // Assert
    Assert.That(library.Entries.Count, Is.EqualTo(updatedCount));
  }
}
