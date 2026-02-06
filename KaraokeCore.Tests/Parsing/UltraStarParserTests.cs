using KaraokeCore.Models;
using KaraokeCore.Parsing;
using NUnit.Framework;

namespace KaraokeCore.Tests.Parsing;

public class UltraStarParserTests
{
  private const string Sample = """
  #TITLE:Silhouette (Filthy Frank Anime OP ver)
  #ARTIST:KANA-BOON
  #LANGUAGE:Japanese
  #GENRE:Anime
  #YEAR:2014
  #CREATOR:Guido Hansen
  #EDITION:Guilty pleasures
  #MP3:KANA-BOON - Silhouette (TV).mp3
  #COVER:papafranku.jpg
  #VIDEO:KANA-BOON - Silhouette (TV).mp4
  #BACKGROUND:filthy.png
  #BPM:366.04
  #GAP:14105
  #MEDLEYSTARTBEAT:768
  #MEDLEYENDBEAT:1785
  : 0 5 6 I
  : 8 6 2 sse
  : 16 6 4  no
  : 24 2 4  se
  : 28 14 6 ~
  - 52
  : 56 6 2 De
  : 64 2 6  fu
  : 68 2 6 mi
  : 72 2 4 ko
  : 76 2 2 mu
  : 80 6 4  goo
  : 88 5 9  ra
  : 94 8 9 in
  : 104 5 4  bo
  : 112 6 1 ku
  : 120 6 4 ra
  : 128 9 2  wa
  - 142
  : 144 2 9 Na
  : 147 4 11 ~
  : 152 6 9 ni
  : 160 9 2 mo
  : 176 2 9  na
  : 179 4 11 ~
  : 184 6 9 ni
  : 192 8 2 mo
  - 206
  : 208 7 2 Ma
  : 216 6 1 ~
  : 224 5 1 da
  : 232 6 2  shi
  : 240 7 4 ra
  : 248 5 2 nu
  - 254
  : 256 5 6 I
  : 264 6 2 ssen
  : 272 3 4  ko
  : 276 3 4 e
  : 280 3 4 te
  : 284 10 6 ~
  - 304
  : 312 2 2 Fu
  : 316 2 2 ri
  : 320 3 6 ka
  : 324 2 6 e
  : 328 2 4 ru
  : 332 2 2  to
  : 336 6 4  mou
  : 344 3 9  na
  : 348 9 9 i
  : 360 6 4  bo
  : 368 6 1 ku
  : 376 2 2 ra
  : 379 4 4 ~
  : 384 9 2  wa
  - 398
  : 400 2 9 Na
  : 403 4 11 ~
  : 408 6 9 ni
  : 416 9 2 mo
  : 432 2 9  na
  : 435 4 11 ~
  : 440 6 9 ni
  : 448 9 2 mo
  - 462
  : 464 7 2 Ma
  : 472 6 1 ~
  : 480 5 1 da
  : 488 6 2  shi
  : 496 7 4 ra
  : 504 8 2 nu
  - 518
  : 520 3 -1 U
  : 524 5 6 da
  : 532 3 4 tte
  : 536 7 6 ~
  : 544 7 2 ~
  : 552 3 2  u
  : 556 5 6 da
  : 564 3 4 tte
  : 568 7 6 ~
  : 576 7 2 ~
  : 584 3 2  u
  : 588 5 11 da
  : 596 3 6 tte
  : 600 5 2 ~
  : 607 9 2 ku
  - 622
  : 624 5 -1 Ki
  : 632 5 1 ra
  : 640 12 2 me
  : 655 14 2 ku
  : 672 4 1  a
  : 680 6 2 se
  : 688 9 4  ga
  - 702
  : 704 11 4 Ko
  : 720 13 4 bo
  : 736 7 6 re
  : 744 7 4 ru
  : 752 4 2  no
  : 760 5 4  sa
  - 766
  : 768 6 2 O
  : 776 6 -3 bo
  : 784 6 2 e
  : 792 6 6 te
  : 800 6 6 nai
  : 808 2 4  ko
  : 812 2 2 to
  : 816 9 4  mo
  - 830
  * 832 6 4 Ta
  * 840 5 -3 ku
  * 848 6 1 sa
  * 856 6 4 n
  * 864 4 7  a
  * 872 2 6 tta
  : 876 3 4  da
  : 880 7 6 rou
  : 888 7 4 ~
  : 896 9 2 ~
  - 910
  : 912 6 11 Da
  : 920 6 9 re
  : 928 10 2 mo
  : 944 6 11  ka
  : 952 6 9 re
  : 960 10 2  mo
  - 975
  : 977 13 2 Shi
  : 992 7 1 ru
  : 1000 7 2 e
  : 1008 6 4 ~
  : 1016 6 6 tto
  - 1023
  : 1025 7 2 Da
  : 1033 6 -3 i
  : 1041 6 2 ji
  : 1049 5 6  ni shi
  : 1057 7 6 ta
  : 1065 2 4 i
  : 1069 2 2  mo
  : 1073 9 4 no
  - 1086
  : 1088 6 4 Mo
  : 1096 6 -3 tte
  : 1104 6 1  o
  : 1112 6 4 to
  : 1120 6 7 na
  : 1128 2 6  ni
  * 1132 2 6  na
  * 1136 6 6 run
  * 1144 6 4 da
  * 1152 8 2 ~
  - 1166
  : 1168 6 11 Do
  : 1176 3 9 n
  : 1180 3 2 ~
  : 1184 10 2 na
  : 1200 6 11  to
  : 1208 6 9 ki
  : 1216 10 2  mo
  - 1230
  : 1232 13 2 Ha
  : 1248 6 1 na
  : 1256 7 9 sa
  : 1264 6 4 zu
  : 1272 5 6  ni
  - 1278
  : 1280 6 2 Ma
  : 1288 6 -3 mo
  : 1296 6 2 ri
  : 1304 5 6 tsu
  : 1312 6 6 zu
  : 1320 2 4 ke
  : 1324 2 2 you
  : 1328 9 4 ~
  - 1342
  : 1344 6 4 So
  : 1352 5 -3 shi
  : 1360 6 1 ta
  : 1368 6 4 ra i
  : 1376 5 7 tsu
  : 1384 5 9  no
  : 1392 6 6  hi
  : 1400 6 4  ni
  : 1408 9 2  ka
  - 1422
  : 1424 6 11 Na
  : 1432 6 9 ni
  : 1440 10 2 mo
  : 1456 6 11 ka
  : 1464 6 9 mo
  : 1472 10 2 ~
  - 1486
  : 1488 7 2 Wa
  : 1496 6 1 ~
  : 1504 7 1 ra
  : 1512 7 2 e
  : 1520 6 4 ru
  : 1528 10 2  sa
  - 1542
  : 1544 3 9 Hi
  : 1548 3 4 ra
  : 1552 6 6 ~
  : 1560 4 4 ri
  : 1568 6 4  to
  : 1577 3 9  hi
  : 1581 3 4 ra
  : 1585 6 6 ~
  : 1593 4 4 ri
  : 1600 6 4  to
  : 1609 3 11  ma
  : 1614 8 6 ~
  : 1624 3 4 tte
  : 1628 3 2 ~
  : 1632 8 2 ru
  - 1646
  : 1648 6 -1 Ko
  : 1656 6 1 no
  : 1664 12 2 ha
  : 1680 12 2  ga
  : 1696 6 6  to
  : 1704 6 4 n
  : 1712 6 2 de
  * 1720 5 -1  yu
  * 1728 7 4 ku
  * 1736 7 4 ~
  * 1744 41 2 ~
  E
  """;

  private const string ExtendedSample = """
  #VERSION:1.1.0
  #TITLE:Night Drive
  #ARTIST:The Examples
  #AUDIO:night-drive.mp3
  #BPM:120.50
  #GAP:2500
  #START:3.5
  #COVER:cover.png
  #BACKGROUND:bg.jpg
  #VIDEO:night-drive.mp4
  #VIDEOGAP:120
  #VOCALS:vocals.mp3
  #INSTRUMENTAL:instrumental.mp3
  #GENRE:Synthwave
  #TAGS:retro;drive;night
  #EDITION:Demo Pack
  #CREATOR:Test Author
  #LANGUAGE:English
  #YEAR:2020
  #END:185000
  #PREVIEWSTART:45.5
  #RELATIVE:YES
  #CALCMEDLEY:NO
  #MEDLEYSTARTBEAT:64
  #MEDLEYENDBEAT:256
  #PROVIDEDBY:ExampleSource
  #APP:UltraStar Deluxe
  #APPVERSION:2023.2
  #SOURCE:Original CD
  : 0 4 0 Hello
  - 4
  E
  """;

  private const string MultiAudioSample = """
  #VERSION:1.1.0
  #TITLE:Legacy Audio
  #ARTIST:Legacy Artist
  #MP3:legacy.mp3
  #BPM:90
  #GAP:0
  : 0 4 0 Yo
  E
  """;

  private const string EdgeCasesSample = """
  #title:Case Insensitive
  #ARTIST:Edge Artist
  #BPM:not-a-number
  #GAP:abc
  #VIDEOGAP:-
  #PREVIEWSTART:NaN
  #RELATIVE:maybe
  #CALCMEDLEY:unknown
  #MEDLEYSTARTBEAT:foo
  #MEDLEYENDBEAT:bar
  #AUDIO:edge.mp3
  P1
  : 0 4 0 hello world
  - 4
  : 5 2 0  spaced text

  E
  """;

  private const string MalformedSample = """
  #TITLE MissingColon
  #ARTIST:Has Artist
  X this should be ignored
  : 1 2
  - 3
  E
  """;

  [Test]
  public void Parse_MetadataAndEventsFromSample_ValuesMatch()
  {
    // Arrange
    const int firstEventIndex = 0;
    const int minimumEventCount = 1;
    var parser = new UltraStarParser();

    // Act
    var song = parser.Parse(Sample.Split('\n'));

    // Assert
    Assert.That(song.Metadata.Title, Is.EqualTo("Silhouette (Filthy Frank Anime OP ver)"));
    Assert.That(song.Metadata.Artist, Is.EqualTo("KANA-BOON"));
    Assert.That(song.Metadata.Language, Is.EqualTo("Japanese"));
    Assert.That(song.Metadata.Genre, Is.EqualTo("Anime"));
    Assert.That(song.Metadata.Year, Is.EqualTo("2014"));
    Assert.That(song.Metadata.Creator, Is.EqualTo("Guido Hansen"));
    Assert.That(song.Metadata.Edition, Is.EqualTo("Guilty pleasures"));
    Assert.That(song.Metadata.Audio, Is.EqualTo("KANA-BOON - Silhouette (TV).mp3"));
    Assert.That(song.Metadata.Cover, Is.EqualTo("papafranku.jpg"));
    Assert.That(song.Metadata.Video, Is.EqualTo("KANA-BOON - Silhouette (TV).mp4"));
    Assert.That(song.Metadata.Background, Is.EqualTo("filthy.png"));
    Assert.That(song.Metadata.Bpm, Is.EqualTo(366.04));
    Assert.That(song.Metadata.GapMs, Is.EqualTo(14105));
    Assert.That(song.Metadata.MedleyStartBeat, Is.EqualTo(768));
    Assert.That(song.Metadata.MedleyEndBeat, Is.EqualTo(1785));

    Assert.That(song.Events.Count, Is.GreaterThan(minimumEventCount));
    Assert.That(song.Events[firstEventIndex], Is.TypeOf<NoteEvent>());
  }

  [Test]
  public void Parse_ExtendedMetadataTags_ValuesMatch()
  {
    // Arrange
    const int expectedEventCount = 2;
    const int firstEventIndex = 0;
    const int secondEventIndex = 1;
    var parser = new UltraStarParser();

    // Act
    var song = parser.Parse(ExtendedSample.Split('\n'));

    // Assert
    Assert.That(song.Metadata.Version, Is.EqualTo("1.1.0"));
    Assert.That(song.Metadata.Title, Is.EqualTo("Night Drive"));
    Assert.That(song.Metadata.Artist, Is.EqualTo("The Examples"));
    Assert.That(song.Metadata.Audio, Is.EqualTo("night-drive.mp3"));
    Assert.That(song.Metadata.Bpm, Is.EqualTo(120.50));
    Assert.That(song.Metadata.GapMs, Is.EqualTo(2500));
    Assert.That(song.Metadata.Cover, Is.EqualTo("cover.png"));
    Assert.That(song.Metadata.Background, Is.EqualTo("bg.jpg"));
    Assert.That(song.Metadata.Video, Is.EqualTo("night-drive.mp4"));
    Assert.That(song.Metadata.VideoGapMs, Is.EqualTo(120));
    Assert.That(song.Metadata.Vocals, Is.EqualTo("vocals.mp3"));
    Assert.That(song.Metadata.Instrumental, Is.EqualTo("instrumental.mp3"));
    Assert.That(song.Metadata.Genre, Is.EqualTo("Synthwave"));
    Assert.That(song.Metadata.Tags, Is.EqualTo("retro;drive;night"));
    Assert.That(song.Metadata.Edition, Is.EqualTo("Demo Pack"));
    Assert.That(song.Metadata.Creator, Is.EqualTo("Test Author"));
    Assert.That(song.Metadata.Language, Is.EqualTo("English"));
    Assert.That(song.Metadata.Year, Is.EqualTo("2020"));
    Assert.That(song.Metadata.EndMs, Is.EqualTo(185000));
    Assert.That(song.Metadata.PreviewStartSeconds, Is.EqualTo(45.5));
    Assert.That(song.Metadata.RelativeTiming, Is.True);
    Assert.That(song.Metadata.CalcMedley, Is.False);
    Assert.That(song.Metadata.MedleyStartBeat, Is.EqualTo(64));
    Assert.That(song.Metadata.MedleyEndBeat, Is.EqualTo(256));

    Assert.That(song.Events.Count, Is.EqualTo(expectedEventCount));
    Assert.That(song.Events[firstEventIndex], Is.TypeOf<NoteEvent>());
    Assert.That(song.Events[secondEventIndex], Is.TypeOf<PhraseEndEvent>());
  }

  [Test]
  public void Parse_AudioTagMissing_UsesMp3Tag()
  {
    // Arrange
    var parser = new UltraStarParser();

    // Act
    var song = parser.Parse(MultiAudioSample.Split('\n'));

    // Assert
    Assert.That(song.Metadata.Audio, Is.EqualTo("legacy.mp3"));
  }

  [Test]
  public void Parse_InvalidNumbersAndCaseInsensitiveTags_Handled()
  {
    // Arrange
    const int playerMarkerIndex = 0;
    const int firstNoteIndex = 1;
    const int phraseEndIndex = 2;
    const int noteWithSpacesIndex = 3;
    var parser = new UltraStarParser();

    // Act
    var song = parser.Parse(EdgeCasesSample.Split('\n'));

    // Assert
    Assert.That(song.Metadata.Title, Is.EqualTo("Case Insensitive"));
    Assert.That(song.Metadata.Artist, Is.EqualTo("Edge Artist"));
    Assert.That(song.Metadata.Audio, Is.EqualTo("edge.mp3"));

    Assert.That(song.Metadata.Bpm, Is.Null);
    Assert.That(song.Metadata.GapMs, Is.Null);
    Assert.That(song.Metadata.VideoGapMs, Is.Null);
    Assert.That(song.Metadata.PreviewStartSeconds, Is.Null);
    Assert.That(song.Metadata.RelativeTiming, Is.Null);
    Assert.That(song.Metadata.CalcMedley, Is.Null);
    Assert.That(song.Metadata.MedleyStartBeat, Is.Null);
    Assert.That(song.Metadata.MedleyEndBeat, Is.Null);

    Assert.That(song.Events[playerMarkerIndex], Is.TypeOf<PlayerMarkerEvent>());
    Assert.That(song.Events[firstNoteIndex], Is.TypeOf<NoteEvent>());
    Assert.That(song.Events[phraseEndIndex], Is.TypeOf<PhraseEndEvent>());
    Assert.That(song.Events[noteWithSpacesIndex], Is.TypeOf<NoteEvent>());

    var noteWithSpaces = (NoteEvent)song.Events[noteWithSpacesIndex];
    Assert.That(noteWithSpaces.Text, Is.EqualTo(" spaced text"));
  }

  [Test]
  public void Parse_UnknownLinesAndMalformedNotes_IgnoredOrDefaulted()
  {
    // Arrange
    const int expectedEventCount = 2;
    const int malformedNoteIndex = 0;
    const int phraseEndIndex = 1;
    const int defaultStartBeat = 0;
    const int defaultLength = 0;
    const int defaultPitch = 0;
    var parser = new UltraStarParser();

    // Act
    var song = parser.Parse(MalformedSample.Split('\n'));

    // Assert
    Assert.That(song.Metadata.Title, Is.Null);
    Assert.That(song.Metadata.Artist, Is.EqualTo("Has Artist"));

    Assert.That(song.Events.Count, Is.EqualTo(expectedEventCount));
    Assert.That(song.Events[malformedNoteIndex], Is.TypeOf<NoteEvent>());
    Assert.That(song.Events[phraseEndIndex], Is.TypeOf<PhraseEndEvent>());

    var malformedNote = (NoteEvent)song.Events[malformedNoteIndex];
    Assert.That(malformedNote.StartBeat, Is.EqualTo(defaultStartBeat));
    Assert.That(malformedNote.Length, Is.EqualTo(defaultLength));
    Assert.That(malformedNote.Pitch, Is.EqualTo(defaultPitch));
    Assert.That(malformedNote.Text, Is.EqualTo(string.Empty));
  }

  [Test]
  public void ParseFromFile_Windows1252CurlyApostrophe_DecodesSuccessfully()
  {
    // Arrange
    var parser = new UltraStarParser();
    var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
    var content = "#TITLE:Song\r\n#BPM:120\r\n#GAP:0\r\n: 0 4 0 I\u2019ll\r\nE\r\n";
    var bytes = EncodeWindows1252(content);

    try
    {
      File.WriteAllBytes(tempPath, bytes);

      // Act
      var song = parser.ParseFromFile(tempPath);

      // Assert
      var note = song.Events.OfType<NoteEvent>().First();
      Assert.That(note.Text, Is.EqualTo("I\u2019ll"));
    }
    finally
    {
      if (File.Exists(tempPath))
      {
        File.Delete(tempPath);
      }
    }
  }

  private static byte[] EncodeWindows1252(string text)
  {
    var bytes = new byte[text.Length];
    for (var i = 0; i < text.Length; i++)
    {
      bytes[i] = text[i] switch
      {
        '\u20AC' => 0x80,
        '\u201A' => 0x82,
        '\u0192' => 0x83,
        '\u201E' => 0x84,
        '\u2026' => 0x85,
        '\u2020' => 0x86,
        '\u2021' => 0x87,
        '\u02C6' => 0x88,
        '\u2030' => 0x89,
        '\u0160' => 0x8A,
        '\u2039' => 0x8B,
        '\u0152' => 0x8C,
        '\u017D' => 0x8E,
        '\u2018' => 0x91,
        '\u2019' => 0x92,
        '\u201C' => 0x93,
        '\u201D' => 0x94,
        '\u2022' => 0x95,
        '\u2013' => 0x96,
        '\u2014' => 0x97,
        '\u02DC' => 0x98,
        '\u2122' => 0x99,
        '\u0161' => 0x9A,
        '\u203A' => 0x9B,
        '\u0153' => 0x9C,
        '\u017E' => 0x9E,
        '\u0178' => 0x9F,
        _ => text[i] <= (char)0xFF ? (byte)text[i] : (byte)'?'
      };
    }

    return bytes;
  }
}
