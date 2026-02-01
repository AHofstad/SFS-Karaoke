using KaraokePlayer.Configuration;
using KaraokePlayer.Presentation;
using NUnit.Framework;

namespace KaraokePlayer.Tests.Presentation;

public class WindowModeHelperTests
{
  [Test]
  public void NextMode_BorderlessFullscreen_ReturnsWindowed()
  {
    // Arrange
    const WindowModeType current = WindowModeType.BorderlessFullscreen;
    const WindowModeType expected = WindowModeType.Windowed;

    // Act
    var next = WindowModeHelper.NextMode(current);

    // Assert
    Assert.That(next, Is.EqualTo(expected));
  }

  [Test]
  public void NextMode_Windowed_ReturnsBorderlessFullscreen()
  {
    // Arrange
    const WindowModeType current = WindowModeType.Windowed;
    const WindowModeType expected = WindowModeType.BorderlessFullscreen;

    // Act
    var next = WindowModeHelper.NextMode(current);

    // Assert
    Assert.That(next, Is.EqualTo(expected));
  }

  
}
