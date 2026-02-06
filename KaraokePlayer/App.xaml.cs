using System.IO;
using System.Windows;

namespace KaraokePlayer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
  private const string _libVlcFolderName = "libvlc";
  private const string _libVlcX64FolderName = "win-x64";
  private const string _libVlcX86FolderName = "win-x86";
  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    var baseLibVlcPath = Path.Combine(AppContext.BaseDirectory, _libVlcFolderName);
    var architectureFolder = Environment.Is64BitProcess ? _libVlcX64FolderName : _libVlcX86FolderName;
    var architecturePath = Path.Combine(baseLibVlcPath, architectureFolder);
    var libVlcPath = Directory.Exists(architecturePath)
      ? architecturePath
      : baseLibVlcPath;
    LibVLCSharp.Shared.Core.Initialize(libVlcPath);
  }
}
