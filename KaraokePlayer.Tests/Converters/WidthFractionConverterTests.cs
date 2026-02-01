using KaraokePlayer.Converters;
using NUnit.Framework;

namespace KaraokePlayer.Tests.Converters;

public class WidthFractionConverterTests
{
  [Test]
  public void Convert_WithFractionParameter_ReturnsScaledWidth()
  {
    // arrange
    const double width = 300;
    const string fraction = "0.333";
    const double expected = 99.9;
    var converter = new WidthFractionConverter();

    // act
    var result = converter.Convert(width, typeof(double), fraction, System.Globalization.CultureInfo.InvariantCulture);

    // assert
    Assert.That(result, Is.EqualTo(expected));
  }

  [Test]
  public void Convert_WithInvalidParameter_UsesDefaultFraction()
  {
    // arrange
    const double width = 300;
    const string fraction = "not-a-number";
    const double expected = 99;
    var converter = new WidthFractionConverter();

    // act
    var result = converter.Convert(width, typeof(double), fraction, System.Globalization.CultureInfo.InvariantCulture);

    // assert
    Assert.That(result, Is.EqualTo(expected));
  }
}
