using System.Globalization;

namespace KaraokeCore.Models;

public sealed class SongMetadata
{
  private readonly Dictionary<string, string> _fields;

  public SongMetadata(Dictionary<string, string> fields)
  {
    _fields = fields;
  }

  public IReadOnlyDictionary<string, string> Fields => _fields;

  public string? Title => Get("TITLE");
  public string? Artist => Get("ARTIST");
  public string? Language => Get("LANGUAGE");
  public string? Genre => Get("GENRE");
  public string? Year => Get("YEAR");
  public string? Creator => Get("CREATOR");
  public string? Edition => Get("EDITION");
  public string? Cover => Get("COVER");
  public string? Background => Get("BACKGROUND");
  public string? Video => Get("VIDEO");
  public string? Vocals => Get("VOCALS");
  public string? Instrumental => Get("INSTRUMENTAL");
  public string? Tags => Get("TAGS");
  public string? Version => Get("VERSION");

  public string? Audio => Get("AUDIO") ?? Get("MP3");

  public double? Bpm => GetDouble("BPM");
  public int? GapMs => GetInt("GAP");
  public int? VideoGapMs => GetInt("VIDEOGAP");
  public int? EndMs => GetInt("END");
  public double? PreviewStartSeconds => GetDouble("PREVIEWSTART");

  public bool? RelativeTiming => GetYesNo("RELATIVE");
  public bool? CalcMedley => GetYesNo("CALCMEDLEY");
  public int? MedleyStartBeat => GetInt("MEDLEYSTARTBEAT");
  public int? MedleyEndBeat => GetInt("MEDLEYENDBEAT");

  private string? Get(string key)
  {
    return _fields.TryGetValue(key, out var value) ? value : null;
  }

  private int? GetInt(string key)
  {
    if (!_fields.TryGetValue(key, out var value))
    {
      return null;
    }

    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
    {
      return result;
    }

    return null;
  }

  private double? GetDouble(string key)
  {
    if (!_fields.TryGetValue(key, out var value))
    {
      return null;
    }

    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
    {
      if (double.IsNaN(result) || double.IsInfinity(result))
      {
        return null;
      }

      return result;
    }

    var normalized = value.Replace(',', '.');
    if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
    {
      if (double.IsNaN(result) || double.IsInfinity(result))
      {
        return null;
      }

      return result;
    }

    return null;
  }

  private bool? GetYesNo(string key)
  {
    if (!_fields.TryGetValue(key, out var value))
    {
      return null;
    }

    if (value.Equals("YES", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (value.Equals("NO", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    return null;
  }
}
