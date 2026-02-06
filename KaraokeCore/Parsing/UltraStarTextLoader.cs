using System.IO.Abstractions;
using System.Text;

namespace KaraokeCore.Parsing;

public static class UltraStarTextLoader
{
  public static string ReadAllText(string path)
  {
    var bytes = File.ReadAllBytes(path);
    return Decode(bytes);
  }

  public static string ReadAllText(IFileSystem fileSystem, string path)
  {
    var bytes = fileSystem.File.ReadAllBytes(path);
    return Decode(bytes);
  }

  public static string[] ReadAllLines(string path)
  {
    return SplitLines(ReadAllText(path));
  }

  public static string[] ReadAllLines(IFileSystem fileSystem, string path)
  {
    return SplitLines(ReadAllText(fileSystem, path));
  }

  private static string Decode(byte[] bytes)
  {
    if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
    {
      return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
    }

    if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
    {
      return Encoding.UTF32.GetString(bytes, 4, bytes.Length - 4);
    }

    if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
    {
      return Encoding.GetEncoding("utf-32BE").GetString(bytes, 4, bytes.Length - 4);
    }

    if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
    {
      return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
    }

    if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
    {
      return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
    }

    var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    try
    {
      return strictUtf8.GetString(bytes);
    }
    catch (DecoderFallbackException)
    {
      // Many UltraStar packs are encoded in Windows-1252; decode that explicitly as fallback.
      return DecodeWindows1252(bytes);
    }
  }

  private static string[] SplitLines(string text)
  {
    return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
  }

  private static string DecodeWindows1252(byte[] bytes)
  {
    var chars = new char[bytes.Length];
    for (var i = 0; i < bytes.Length; i++)
    {
      chars[i] = bytes[i] switch
      {
        0x80 => '\u20AC',
        0x82 => '\u201A',
        0x83 => '\u0192',
        0x84 => '\u201E',
        0x85 => '\u2026',
        0x86 => '\u2020',
        0x87 => '\u2021',
        0x88 => '\u02C6',
        0x89 => '\u2030',
        0x8A => '\u0160',
        0x8B => '\u2039',
        0x8C => '\u0152',
        0x8E => '\u017D',
        0x91 => '\u2018',
        0x92 => '\u2019',
        0x93 => '\u201C',
        0x94 => '\u201D',
        0x95 => '\u2022',
        0x96 => '\u2013',
        0x97 => '\u2014',
        0x98 => '\u02DC',
        0x99 => '\u2122',
        0x9A => '\u0161',
        0x9B => '\u203A',
        0x9C => '\u0153',
        0x9E => '\u017E',
        0x9F => '\u0178',
        _ => (char)bytes[i]
      };
    }

    return new string(chars);
  }
}
