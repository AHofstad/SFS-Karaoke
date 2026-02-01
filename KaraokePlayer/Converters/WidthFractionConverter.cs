using System;
using System.Globalization;
using System.Windows.Data;

namespace KaraokePlayer.Converters;

public sealed class WidthFractionConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (!TryGetDouble(value, out var width))
    {
      return 0d;
    }

    var fraction = 0.33d;
    if (parameter is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
    {
      fraction = parsed;
    }

    return Math.Max(0d, width * fraction);
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }

  private static bool TryGetDouble(object value, out double result)
  {
    if (value is double doubleValue)
    {
      result = doubleValue;
      return true;
    }

    if (value is float floatValue)
    {
      result = floatValue;
      return true;
    }

    if (value is int intValue)
    {
      result = intValue;
      return true;
    }

    result = 0d;
    return false;
  }
}
