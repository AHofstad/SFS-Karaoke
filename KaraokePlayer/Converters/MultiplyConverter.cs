using System;
using System.Globalization;
using System.Windows.Data;

namespace KaraokePlayer.Converters;

public sealed class MultiplyConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is not double number)
    {
      return 0d;
    }

    if (parameter is not double factor)
    {
      return number;
    }

    return number * factor;
  }

  public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
