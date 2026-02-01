using System.Windows;
using System.Windows.Controls;

namespace KaraokePlayer.Views;

public partial class MainMenuView : System.Windows.Controls.UserControl
{
  public MainMenuView()
  {
    InitializeComponent();
  }

  public static readonly RoutedEvent EnterPlayerRequestedEvent =
    EventManager.RegisterRoutedEvent(
      nameof(EnterPlayerRequested),
      RoutingStrategy.Bubble,
      typeof(RoutedEventHandler),
      typeof(MainMenuView));

  public static readonly RoutedEvent OptionsRequestedEvent =
    EventManager.RegisterRoutedEvent(
      nameof(OptionsRequested),
      RoutingStrategy.Bubble,
      typeof(RoutedEventHandler),
      typeof(MainMenuView));

  public static readonly RoutedEvent ExitRequestedEvent =
    EventManager.RegisterRoutedEvent(
      nameof(ExitRequested),
      RoutingStrategy.Bubble,
      typeof(RoutedEventHandler),
      typeof(MainMenuView));

  public event RoutedEventHandler EnterPlayerRequested
  {
    add => AddHandler(EnterPlayerRequestedEvent, value);
    remove => RemoveHandler(EnterPlayerRequestedEvent, value);
  }

  public event RoutedEventHandler OptionsRequested
  {
    add => AddHandler(OptionsRequestedEvent, value);
    remove => RemoveHandler(OptionsRequestedEvent, value);
  }

  public event RoutedEventHandler ExitRequested
  {
    add => AddHandler(ExitRequestedEvent, value);
    remove => RemoveHandler(ExitRequestedEvent, value);
  }

  private void EnterPlayer_Click(object sender, RoutedEventArgs e)
  {
    RaiseEvent(new RoutedEventArgs(EnterPlayerRequestedEvent));
  }

  private void Options_Click(object sender, RoutedEventArgs e)
  {
    RaiseEvent(new RoutedEventArgs(OptionsRequestedEvent));
  }

  private void Exit_Click(object sender, RoutedEventArgs e)
  {
    RaiseEvent(new RoutedEventArgs(ExitRequestedEvent));
  }
}
