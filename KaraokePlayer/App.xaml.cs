using System;
using System.IO;
using System.Windows;
namespace KaraokePlayer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
  private const string LibVlcFolderName = "libvlc";
  private const string LibVlcX64FolderName = "win-x64";
  private const string LibVlcX86FolderName = "win-x86";
  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    var baseLibVlcPath = Path.Combine(AppContext.BaseDirectory, LibVlcFolderName);
    var architectureFolder = Environment.Is64BitProcess ? LibVlcX64FolderName : LibVlcX86FolderName;
    var architecturePath = Path.Combine(baseLibVlcPath, architectureFolder);
    var libVlcPath = Directory.Exists(architecturePath)
      ? architecturePath
      : baseLibVlcPath;
    LibVLCSharp.Shared.Core.Initialize(libVlcPath);
  }
}
