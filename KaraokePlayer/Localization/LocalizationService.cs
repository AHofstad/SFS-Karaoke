using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using KaraokePlayer.Configuration;
using KaraokePlayer.Resources;

namespace KaraokePlayer.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
  private const string _defaultLanguageCode = "en-US";
  private readonly SettingsService _settingsService;

  private LanguageOption _currentLanguage;

  public static LocalizationService Instance { get; } = new(SettingsService.Instance);

  public LocalizationService(SettingsService settingsService)
  {
    _settingsService = settingsService;
    Languages = new ObservableCollection<LanguageOption>
    {
      new("en-US", "English"),
      new("nl-NL", "Nederlands"),
    };

    var settings = _settingsService.Load();
    _currentLanguage = ResolveLanguage(settings.LanguageCode) ?? Languages[0];
    ApplyCulture(_currentLanguage.Code);
  }

  public ObservableCollection<LanguageOption> Languages { get; }

  public LanguageOption CurrentLanguage
  {
    get => _currentLanguage;
    set
    {
      if (value == _currentLanguage)
      {
        return;
      }

      _currentLanguage = value;
      ApplyCulture(_currentLanguage.Code);
      _settingsService.Save(new AppSettings { LanguageCode = _currentLanguage.Code });
      OnPropertyChanged();
      OnPropertyChanged(_indexerPropertyName);
    }
  }

  public string this[string key]
  {
    get
    {
      var value = Strings.ResourceManager.GetString(key, Strings.Culture);
      return string.IsNullOrWhiteSpace(value) ? key : value;
    }
  }

  public event PropertyChangedEventHandler? PropertyChanged;

  private void ApplyCulture(string? cultureCode)
  {
    var code = string.IsNullOrWhiteSpace(cultureCode) ? _defaultLanguageCode : cultureCode;
    var culture = new CultureInfo(code);

    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
    Strings.Culture = culture;
  }

  private LanguageOption? ResolveLanguage(string? cultureCode)
  {
    if (string.IsNullOrWhiteSpace(cultureCode))
    {
      return null;
    }

    return Languages.FirstOrDefault(language => language.Code.Equals(cultureCode, StringComparison.OrdinalIgnoreCase));
  }

  private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
  {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
  }

  private const string _indexerPropertyName = "Item[]";
}
