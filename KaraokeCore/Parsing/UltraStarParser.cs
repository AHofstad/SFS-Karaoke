using System.Globalization;
using System.Text;
using KaraokeCore.Models;

namespace KaraokeCore.Parsing;

public sealed class UltraStarParser
{
  private const int LeadIndex = 0;
  private const int MetadataKeyStartIndex = 1;
  private const int MinimumColonIndex = 1;
  private const int MinimumKeyLength = 1;
  private const int ValueOffsetFromColon = 1;
  private const int NoteMinimumParts = 4;
  private const int FirstPartIndex = 1;
  private const int SecondPartIndex = 2;
  private const int ThirdPartIndex = 3;
  private const int FourthPartIndex = 4;
  private const int PhraseMinimumParts = 2;
  private const int PlayerMarkerMinimumLength = 2;
  private const int NotePartsCapacity = 5;
  private const int MaxSpacesInNote = 4;
  private const int DefaultIntValue = 0;

  public UltraStarSong ParseFromFile(string path)
  {
    var encoding = new UTF8Encoding(false, true);
    var lines = File.ReadLines(path, encoding);
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

      var lead = line[LeadIndex];
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
    if (colonIndex <= MinimumColonIndex)
    {
      return;
    }

    var key = line[MetadataKeyStartIndex..colonIndex].Trim();
    if (key.Length < MinimumKeyLength)
    {
      return;
    }

    var value = line[(colonIndex + ValueOffsetFromColon)..].Trim();
    metadata[key] = value;
  }

  private static NoteEvent ParseNoteLine(string line)
  {
    var noteType = line[LeadIndex] switch
    {
      ':' => NoteType.Normal,
      '*' => NoteType.Golden,
      'F' => NoteType.Freestyle,
      'R' => NoteType.Rap,
      'G' => NoteType.RapGolden,
      _ => NoteType.Normal,
    };

    var parts = SplitNoteLine(line);
    if (parts.Length < NoteMinimumParts)
    {
      return new NoteEvent(noteType, DefaultIntValue, DefaultIntValue, DefaultIntValue, string.Empty);
    }

    var startBeat = ParseInt(parts[FirstPartIndex]);
    var length = ParseInt(parts[SecondPartIndex]);
    var pitch = ParseInt(parts[ThirdPartIndex]);
    var text = parts.Length > NoteMinimumParts ? parts[FourthPartIndex] : string.Empty;

    return new NoteEvent(noteType, startBeat, length, pitch, text);
  }

  private static PhraseEndEvent? ParsePhraseEnd(string line)
  {
    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < PhraseMinimumParts)
    {
      return null;
    }

    var startBeat = ParseInt(parts[FirstPartIndex]);
    return new PhraseEndEvent(startBeat);
  }

  private static PlayerMarkerEvent? ParsePlayerMarker(string line)
  {
    var marker = line.Trim();
    if (marker.Length < PlayerMarkerMinimumLength)
    {
      return null;
    }

    return new PlayerMarkerEvent(marker);
  }

  private static string[] SplitNoteLine(string line)
  {
    var parts = new List<string>(NotePartsCapacity);
    var current = new StringBuilder();
    var spaceCount = DefaultIntValue;

    for (var i = DefaultIntValue; i < line.Length; i++)
    {
      var ch = line[i];
      if (ch == ' ' && spaceCount < MaxSpacesInNote)
      {
        if (current.Length > DefaultIntValue)
        {
          parts.Add(current.ToString());
          current.Clear();
          spaceCount++;
        }
        continue;
      }

      current.Append(ch);
    }

    if (current.Length > DefaultIntValue)
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

    return DefaultIntValue;
  }
}
