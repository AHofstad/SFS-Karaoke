using System;
using System.Windows;

namespace KaraokePlayer.Views;

public partial class GameOverlayWindow : Window
{
  public event EventHandler? ScrubStarted;
  public event EventHandler<double>? ScrubCompleted;
  public event EventHandler? PauseResumeRequested;
  public event EventHandler? PauseRestartRequested;
  public event EventHandler? PauseQuitRequested;
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

  public bool IsPauseMenuVisible => PauseMenuOverlay.Visibility == Visibility.Visible;

  public void ShowPauseMenu()
  {
    PauseMenuOverlay.Visibility = Visibility.Visible;
  }

  public void HidePauseMenu()
  {
    PauseMenuOverlay.Visibility = Visibility.Collapsed;
  }

  private void PauseResume_Click(object sender, RoutedEventArgs e)
  {
    PauseResumeRequested?.Invoke(this, EventArgs.Empty);
  }

  private void PauseRestart_Click(object sender, RoutedEventArgs e)
  {
    PauseRestartRequested?.Invoke(this, EventArgs.Empty);
  }

  private void PauseQuit_Click(object sender, RoutedEventArgs e)
  {
    PauseQuitRequested?.Invoke(this, EventArgs.Empty);
  }
}
