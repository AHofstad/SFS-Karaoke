using KaraokePlayer.Converters;
using NUnit.Framework;

namespace KaraokePlayer.Tests.Converters;

public class PercentToWidthConverterTests
{
  [Test]
  public void Convert_WithValidValues_ReturnsScaledWidth()
  {
    // arrange
    const double width = 200;
    const double value = 50;
    const double maximum = 100;
    const double expected = 100;
    var converter = new PercentToWidthConverter();
    var values = new object[] { width, value, maximum };

    // act
    var result = converter.Convert(values, typeof(double), string.Empty, System.Globalization.CultureInfo.InvariantCulture);

    // assert
    Assert.That(result, Is.EqualTo(expected));
  }

  [Test]
  public void Convert_WithValueAboveMaximum_ClampsToWidth()
  {
    // arrange
    const double width = 180;
    const double value = 250;
    const double maximum = 100;
    const double expected = 180;
    var converter = new PercentToWidthConverter();
    var values = new object[] { width, value, maximum };

    // act
    var result = converter.Convert(values, typeof(double), string.Empty, System.Globalization.CultureInfo.InvariantCulture);

    // assert
    Assert.That(result, Is.EqualTo(expected));
  }
}
