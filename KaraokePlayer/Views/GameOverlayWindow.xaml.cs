using System;
using System.Windows;

namespace KaraokePlayer.Views;

public partial class GameOverlayWindow : Window
{
  public event EventHandler? ScrubStarted;
  public event EventHandler<double>? ScrubCompleted;
  private bool _isScrubbing;

  public GameOverlayWindow()
  {
    InitializeComponent();
  }

  private void GameScrubSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    if (_isScrubbing)
    {
      return;
    }

    _isScrubbing = true;
    ScrubStarted?.Invoke(this, EventArgs.Empty);
  }

  private void GameScrubSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
  {
    if (!_isScrubbing)
    {
      return;
    }

    _isScrubbing = false;
    ScrubCompleted?.Invoke(this, GameScrubSlider.Value);
  }
}
