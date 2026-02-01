using System.Windows;

namespace KaraokePlayer.Views;

public partial class QueueOverlayWindow : Window
{
  public QueueOverlayWindow()
  {
    InitializeComponent();
  }

  public void SetBarHeight(double height)
  {
    if (height < 0)
    {
      height = 0;
    }

    BottomBar.Height = height;
  }
}
