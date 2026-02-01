using System.Windows;
using System.Windows.Controls;
using KaraokePlayer.Presentation;

namespace KaraokePlayer.Views;

public partial class OptionsView : System.Windows.Controls.UserControl
{
  public OptionsView()
  {
    InitializeComponent();
  }

  public static readonly RoutedEvent CloseRequestedEvent =
    EventManager.RegisterRoutedEvent(
      nameof(CloseRequested),
      RoutingStrategy.Bubble,
      typeof(RoutedEventHandler),
      typeof(OptionsView));

  public static readonly RoutedEvent BrowseRequestedEvent =
    EventManager.RegisterRoutedEvent(
      nameof(BrowseRequested),
      RoutingStrategy.Bubble,
      typeof(RoutedEventHandler),
      typeof(OptionsView));

  public event RoutedEventHandler CloseRequested
  {
    add => AddHandler(CloseRequestedEvent, value);
    remove => RemoveHandler(CloseRequestedEvent, value);
  }

  public event RoutedEventHandler BrowseRequested
  {
    add => AddHandler(BrowseRequestedEvent, value);
    remove => RemoveHandler(BrowseRequestedEvent, value);
  }

  private void CloseOptions_Click(object sender, RoutedEventArgs e)
  {
    if (DataContext is OptionsViewModel options)
    {
      options.Save();
    }

    RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));
  }

  private void BrowseSongsFolder_Click(object sender, RoutedEventArgs e)
  {
    RaiseEvent(new RoutedEventArgs(BrowseRequestedEvent));
  }
}
