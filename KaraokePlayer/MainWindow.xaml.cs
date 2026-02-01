using System.Windows;
using System.Windows.Input;
using KaraokePlayer.Presentation;
using KaraokePlayer.Web;

namespace KaraokePlayer;

public partial class MainWindow : Window
{
  private const int DefaultWindowedWidth = 1280;
  private const int DefaultWindowedHeight = 720;
  private const WindowStyle BorderlessStyle = WindowStyle.None;
  private const WindowStyle WindowedStyle = WindowStyle.SingleBorderWindow;
  private const ResizeMode BorderlessResize = ResizeMode.NoResize;
  private const ResizeMode WindowedResize = ResizeMode.CanResize;
  private readonly MainViewModel _viewModel = new();
  private readonly QueueWebHost _queueWebHost;
  public static readonly RoutedCommand SkipToFirstNoteCommand = new();
  public static readonly RoutedCommand EnterActionCommand = new();
  public static readonly RoutedCommand PreviousSongCommand = new();
  public static readonly RoutedCommand NextSongCommand = new();
  public static readonly RoutedCommand ToggleWindowModeCommand = new();
  public static readonly RoutedCommand BackCommand = new();
  private Configuration.WindowModeType _windowMode = Configuration.WindowModeType.BorderlessFullscreen;
  private bool _isUpdatingSize;

  public MainWindow()
  {
    InitializeComponent();
    DataContext = _viewModel;
    _queueWebHost = new QueueWebHost(_viewModel);
    Loaded += OnLoaded;
    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    _viewModel.Options.WindowModeChanged += Options_WindowModeChanged;
    _viewModel.Options.SongsFolderChanged += Options_SongsFolderChanged;
  }

  private void OnLoaded(object sender, RoutedEventArgs e)
  {
    LoadWindowMode();
    _viewModel.ShowMainMenu();
    _viewModel.LoadSongs();
    _ = _queueWebHost.StartAsync();
  }

  private void ReloadLibrary_Click(object sender, RoutedEventArgs e)
  {
    _viewModel.LoadSongs();
  }

  private void SkipToFirstNote_Executed(object sender, ExecutedRoutedEventArgs e)
  {
    _viewModel.GameView.RequestSkipToFirstNote();
  }

  private void SkipToFirstNote_CanExecute(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = _viewModel.CurrentView is Presentation.GameViewModel && _viewModel.CurrentQueueSong is not null;
  }

  private void ToggleWindowMode_Click(object sender, RoutedEventArgs e)
  {
    ToggleWindowMode();
  }

  private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
  {
    const string hasSelectedSongProperty = nameof(MainViewModel.HasSelectedSong);
    if (e.PropertyName == hasSelectedSongProperty)
    {
      CommandManager.InvalidateRequerySuggested();
    }
  }


  private void MainMenuView_EnterPlayerRequested(object sender, RoutedEventArgs e)
  {
    _viewModel.ShowQueue();
  }

  private void MainMenuView_OptionsRequested(object sender, RoutedEventArgs e)
  {
    _viewModel.ShowOptions();
  }

  private void MainMenuView_ExitRequested(object sender, RoutedEventArgs e)
  {
    Close();
  }

  private void OptionsView_CloseRequested(object sender, RoutedEventArgs e)
  {
    _viewModel.Options.Save();
    LoadWindowMode();
    _viewModel.ShowMainMenu();
  }

  private void OptionsView_BrowseRequested(object sender, RoutedEventArgs e)
  {
    var dialog = new System.Windows.Forms.FolderBrowserDialog
    {
      Description = KaraokePlayer.Resources.Strings.SongsFolderDialogTitle,
      ShowNewFolderButton = true
    };

    var result = dialog.ShowDialog();
    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
    {
      _viewModel.Options.UpdateSongsFolder(dialog.SelectedPath);
    }
  }

  private void QueueView_StartKaraokeRequested(object sender, RoutedEventArgs e)
  {
    if (_viewModel.CanStartKaraoke())
    {
      _viewModel.StartKaraoke();
    }
  }

  private void EnterAction_Executed(object sender, ExecutedRoutedEventArgs e)
  {
    if (_viewModel.CurrentView is Presentation.QueueViewModel)
    {
      _viewModel.AddSelectedToQueue();
      return;
    }

    if (_viewModel.CurrentView is Presentation.GameViewModel)
    {
      _viewModel.GameView.RequestSkipToFirstNote();
    }
  }

  private void EnterAction_CanExecute(object sender, CanExecuteRoutedEventArgs e)
  {
    if (_viewModel.CurrentView is Presentation.QueueViewModel)
    {
      e.CanExecute = _viewModel.CanAddToQueue();
      return;
    }

    if (_viewModel.CurrentView is Presentation.GameViewModel)
    {
      e.CanExecute = _viewModel.HasSelectedSong;
      return;
    }

    e.CanExecute = false;
  }

  private void PreviousSong_Executed(object sender, ExecutedRoutedEventArgs e)
  {
    _viewModel.SelectPreviousSong();
  }

  private void PreviousSong_CanExecute(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = _viewModel.CurrentView is Presentation.QueueViewModel && _viewModel.Songs.Count > 0;
  }

  private void NextSong_Executed(object sender, ExecutedRoutedEventArgs e)
  {
    _viewModel.SelectNextSong();
  }

  private void NextSong_CanExecute(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = _viewModel.CurrentView is Presentation.QueueViewModel && _viewModel.Songs.Count > 0;
  }

  private void Options_SongsFolderChanged(object? sender, string path)
  {
    _viewModel.LoadSongs();
  }

  private void ToggleWindowMode_Executed(object sender, ExecutedRoutedEventArgs e)
  {
    ToggleWindowMode();
  }

  private void ToggleWindowMode()
  {
    _windowMode = Presentation.WindowModeHelper.NextMode(_windowMode);

    ApplyWindowMode(_windowMode);
    SaveWindowMode(_windowMode);
  }

  private void ApplyWindowMode(Configuration.WindowModeType mode)
  {
    switch (mode)
    {
      case Configuration.WindowModeType.Windowed:
        var settings = Configuration.SettingsService.Instance.Load();
        WindowState = WindowState.Normal;
        WindowStyle = WindowedStyle;
        ResizeMode = WindowedResize;
        Width = settings.WindowedWidth ?? DefaultWindowedWidth;
        Height = settings.WindowedHeight ?? DefaultWindowedHeight;
        break;
      default:
        WindowStyle = BorderlessStyle;
        ResizeMode = BorderlessResize;
        WindowState = WindowState.Maximized;
        break;
    }
  }

  private void SaveWindowMode(Configuration.WindowModeType mode)
  {
    var settingsService = Configuration.SettingsService.Instance;
    var settings = settingsService.Load();
    settings.WindowMode = mode.ToString();
    settingsService.Save(settings);
  }

  private void LoadWindowMode()
  {
    var settingsService = Configuration.SettingsService.Instance;
    var settings = settingsService.Load();
    if (Enum.TryParse<Configuration.WindowModeType>(settings.WindowMode, true, out var parsed))
    {
      _windowMode = parsed;
    }
    else
    {
      _windowMode = Configuration.WindowModeType.BorderlessFullscreen;
    }

    ApplyWindowMode(_windowMode);
  }

  private void Options_WindowModeChanged(object? sender, Configuration.WindowModeType mode)
  {
    _windowMode = mode;
    ApplyWindowMode(_windowMode);
  }

  private void Back_Executed(object sender, ExecutedRoutedEventArgs e)
  {
    if (_viewModel.CurrentView is Presentation.OptionsViewModel)
    {
      _viewModel.Options.Save();
      LoadWindowMode();
    }

    _viewModel.NavigateBack();
  }

  private void Back_CanExecute(object sender, CanExecuteRoutedEventArgs e)
  {
    e.CanExecute = _viewModel.CanNavigateBack();
  }

  protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
  {
    base.OnRenderSizeChanged(sizeInfo);
    if (_isUpdatingSize || _windowMode != Configuration.WindowModeType.Windowed)
    {
      return;
    }

    try
    {
      _isUpdatingSize = true;
      var settingsService = Configuration.SettingsService.Instance;
      var settings = settingsService.Load();
      settings.WindowedWidth = (int)Width;
      settings.WindowedHeight = (int)Height;
      settingsService.Save(settings);
    }
    finally
    {
      _isUpdatingSize = false;
    }
  }

  protected override async void OnClosed(EventArgs e)
  {
    await _queueWebHost.DisposeAsync();
    base.OnClosed(e);
  }

}
