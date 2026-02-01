using System;
using System.Globalization;
using System.Windows.Data;

namespace KaraokePlayer.Converters;

public sealed class PercentToWidthConverter : IMultiValueConverter
{
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    if (values.Length < 3)
    {
      return 0d;
    }

    if (!TryGetDouble(values[0], out var width))
    {
      return 0d;
    }

    if (!TryGetDouble(values[1], out var value))
    {
      return 0d;
    }

    if (!TryGetDouble(values[2], out var maximum))
    {
      return 0d;
    }

    if (width <= 0 || maximum <= 0)
    {
      return 0d;
    }

    var percent = Math.Clamp(value / maximum, 0d, 1d);
    return width * percent;
  }

  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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
