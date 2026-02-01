using KaraokeCore.Parsing;
using KaraokePlayer.Presentation;
using NUnit.Framework;

namespace KaraokePlayer.Tests.Presentation;

public class LyricLineBuilderTests
{
  [Test]
  public void BuildLines_WithPhraseEnd_CreatesSingleLineWithCombinedText()
  {
    // arrange
    var parser = new UltraStarParser();
    var lines = new[]
    {
      "#TITLE:Song",
      "#BPM:120",
      "#GAP:0",
      ": 0 4 0 He",
      ": 4 4 0 llo",
      "- 8",
      "E"
    };

    // act
    var song = parser.Parse(lines);
    var result = LyricLineBuilder.BuildLines(song);

    // assert
    Assert.That(result.Count, Is.EqualTo(1));
    Assert.That(result[0].Text, Is.EqualTo("He llo"));
    Assert.That(result[0].Tokens.Count, Is.EqualTo(2));
    Assert.That(result[0].Tokens[0].Text, Is.EqualTo("He"));
    Assert.That(result[0].Tokens[1].Text, Is.EqualTo("llo"));
    Assert.That(result[0].StartMs, Is.EqualTo(0));
    Assert.That(result[0].EndMs, Is.GreaterThan(0));
  }

  [Test]
  public void BuildLines_MultiplePhrases_ReturnsTwoLines()
  {
    // arrange
    var parser = new UltraStarParser();
    var lines = new[]
    {
      "#TITLE:Song",
      "#BPM:60",
      "#GAP:0",
      ": 0 4 0 Hi",
      "- 4",
      ": 4 4 0 There",
      "- 8",
      "E"
    };

    // act
    var song = parser.Parse(lines);
    var result = LyricLineBuilder.BuildLines(song);

    // assert
    Assert.That(result.Count, Is.EqualTo(2));
    Assert.That(result[0].Text, Is.EqualTo("Hi"));
    Assert.That(result[1].Text, Is.EqualTo("There"));
    Assert.That(result[0].Tokens.Count, Is.EqualTo(1));
    Assert.That(result[1].Tokens.Count, Is.EqualTo(1));
  }

  [Test]
  public void BuildLines_WithoutPhraseEnd_UsesLastNoteAsLineEnd()
  {
    // arrange
    var parser = new UltraStarParser();
    var lines = new[]
    {
      "#TITLE:Song",
      "#BPM:120",
      "#GAP:0",
      ": 0 4 0 Hey",
      ": 4 4 0 you",
      "E"
    };

    // act
    var song = parser.Parse(lines);
    var result = LyricLineBuilder.BuildLines(song);

    // assert
    Assert.That(result.Count, Is.EqualTo(1));
    Assert.That(result[0].Text, Is.EqualTo("Hey you"));
    Assert.That(result[0].Tokens.Count, Is.EqualTo(2));
  }

  [Test]
  public void BuildLines_MultipleNotes_AddsSpacesBetweenParts()
  {
    // arrange
    var parser = new UltraStarParser();
    var lines = new[]
    {
      "#TITLE:Song",
      "#BPM:120",
      "#GAP:0",
      ": 0 4 0 Don",
      ": 4 4 0 't",
      ": 8 4 0 look",
      "- 12",
      "E"
    };

    // act
    var song = parser.Parse(lines);
    var result = LyricLineBuilder.BuildLines(song);

    // assert
    Assert.That(result.Count, Is.EqualTo(1));
    Assert.That(result[0].Text, Is.EqualTo("Don 't look"));
    Assert.That(result[0].Tokens.Count, Is.EqualTo(3));
  }
}
