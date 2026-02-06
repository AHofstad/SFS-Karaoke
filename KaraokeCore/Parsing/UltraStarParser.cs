using System.Globalization;
using System.Text;
using KaraokeCore.Models;

namespace KaraokeCore.Parsing;

public sealed class UltraStarParser
{
  private const int _leadIndex = 0;
  private const int _metadataKeyStartIndex = 1;
  private const int _minimumColonIndex = 1;
  private const int _minimumKeyLength = 1;
  private const int _valueOffsetFromColon = 1;
  private const int _noteMinimumParts = 4;
  private const int _firstPartIndex = 1;
  private const int _secondPartIndex = 2;
  private const int _thirdPartIndex = 3;
  private const int _fourthPartIndex = 4;
  private const int _phraseMinimumParts = 2;
  private const int _playerMarkerMinimumLength = 2;
  private const int _notePartsCapacity = 5;
  private const int _maxSpacesInNote = 4;
  private const int _defaultIntValue = 0;

  public UltraStarSong ParseFromFile(string path)
  {
    var lines = UltraStarTextLoader.ReadAllLines(path);
    return Parse(lines);
  }

  public UltraStarSong Parse(IEnumerable<string> lines)
  {
    var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var events = new List<IUltraStarEvent>();

    foreach (var rawLine in lines)
    {
      var line = rawLine.Trim();

      if (string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      if (line.StartsWith('#'))
      {
        ParseMetadata(line, metadata);
        continue;
      }

      var lead = line[_leadIndex];
      if (lead == ':' || lead == '*' || lead == 'F' || lead == 'R' || lead == 'G')
      {
        var note = ParseNoteLine(line);
        events.Add(note);
        continue;
      }

      if (lead == '-')
      {
        var phrase = ParsePhraseEnd(line);
        if (phrase != null)
        {
          events.Add(phrase);
        }
        continue;
      }

      if (lead == 'P')
      {
        var marker = ParsePlayerMarker(line);
        if (marker != null)
        {
          events.Add(marker);
        }
        continue;
      }

      if (lead == 'E')
      {
        break;
      }
    }

    return new UltraStarSong(new SongMetadata(metadata), events);
  }

  private static void ParseMetadata(string line, Dictionary<string, string> metadata)
  {
    var colonIndex = line.IndexOf(':');
    if (colonIndex <= _minimumColonIndex)
    {
      return;
    }

    var key = line[_metadataKeyStartIndex..colonIndex].Trim();
    if (key.Length < _minimumKeyLength)
    {
      return;
    }

    var value = line[(colonIndex + _valueOffsetFromColon)..].Trim();
    metadata[key] = value;
  }

  private static NoteEvent ParseNoteLine(string line)
  {
    var noteType = line[_leadIndex] switch
    {
      ':' => NoteType.Normal,
      '*' => NoteType.Golden,
      'F' => NoteType.Freestyle,
      'R' => NoteType.Rap,
      'G' => NoteType.RapGolden,
      _ => NoteType.Normal,
    };

    var parts = SplitNoteLine(line);
    if (parts.Length < _noteMinimumParts)
    {
      return new NoteEvent(noteType, _defaultIntValue, _defaultIntValue, _defaultIntValue, string.Empty);
    }

    var startBeat = ParseInt(parts[_firstPartIndex]);
    var length = ParseInt(parts[_secondPartIndex]);
    var pitch = ParseInt(parts[_thirdPartIndex]);
    var text = parts.Length > _noteMinimumParts ? parts[_fourthPartIndex] : string.Empty;

    return new NoteEvent(noteType, startBeat, length, pitch, text);
  }

  private static PhraseEndEvent? ParsePhraseEnd(string line)
  {
    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < _phraseMinimumParts)
    {
      return null;
    }

    var startBeat = ParseInt(parts[_firstPartIndex]);
    return new PhraseEndEvent(startBeat);
  }

  private static PlayerMarkerEvent? ParsePlayerMarker(string line)
  {
    var marker = line.Trim();
    if (marker.Length < _playerMarkerMinimumLength)
    {
      return null;
    }

    return new PlayerMarkerEvent(marker);
  }

  private static string[] SplitNoteLine(string line)
  {
    var parts = new List<string>(_notePartsCapacity);
    var current = new StringBuilder();
    var spaceCount = _defaultIntValue;

    for (var i = _defaultIntValue; i < line.Length; i++)
    {
      var ch = line[i];
      if (ch == ' ' && spaceCount < _maxSpacesInNote)
      {
        if (current.Length > _defaultIntValue)
        {
          parts.Add(current.ToString());
          current.Clear();
          spaceCount++;
        }
        continue;
      }

      current.Append(ch);
    }

    if (current.Length > _defaultIntValue)
    {
      parts.Add(current.ToString());
    }

    return parts.ToArray();
  }

  private static int ParseInt(string value)
  {
    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
    {
      return result;
    }

    return _defaultIntValue;
  }
}
