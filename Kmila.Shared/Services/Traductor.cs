using System.Text.Json;
using Microsoft.JSInterop;

namespace Kmila.Shared.Services;

/// <summary>
/// Translation service for internationalization (i18n) and localization (l10n) support.
/// Provides multi-language support for the Kmila application with dynamic language switching.
/// </summary>
/// <remarks>
/// <para>
/// The Traductor (Spanish for "Translator") service manages translations for the Kmila-9s application,
/// enabling the UI to be displayed in multiple languages for accessibility to students worldwide.
/// </para>
/// <para>
/// <b>Supported Languages:</b>
/// <list type="bullet">
/// <item><description>English (en) - Default</description></item>
/// <item><description>Spanish (es)</description></item>
/// <item><description>French (fr)</description></item>
/// <item><description>German (de)</description></item>
/// <item><description>Simplified Chinese (ch)</description></item>
/// <item><description>Japanese (jp)</description></item>
/// <item><description>Arabic (ar)</description></item>
/// </list>
/// </para>
/// </remarks>
public class Traductor : IDisposable
{
    #region Constants

    /// <summary>
    /// The storage key used for persisting language preference.
    /// </summary>
    public const string LANGUAGE_KEY = "KMILA_LANGUAGE";

    /// <summary>
    /// The default language code when no preference is set.
    /// </summary>
    public const string DEFAULT_LANGUAGE = "en";

    #endregion

    #region Properties

    /// <summary>
    /// Gets the last error message encountered during translation operations.
    /// </summary>
    public string LastError { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current language code.
    /// </summary>
    public string Lang => _lang;

    /// <summary>
    /// Gets whether the current language is right-to-left (RTL).
    /// </summary>
    public bool IsRtl => _lang == "ar";

    /// <summary>
    /// Gets whether the service has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the language changes. Components should subscribe to this
    /// and call StateHasChanged() when notified.
    /// </summary>
    public event Action? OnLangChanged;

    #endregion

    #region Private Fields

    private Dictionary<string, string> _languageKeys = new();
    private string _lang = DEFAULT_LANGUAGE;
    private bool _isInitialized = false;
    private readonly Func<string, Task<Stream>> _fileReader;

    /// <summary>
    /// Available languages with their display names.
    /// </summary>
    public static readonly Dictionary<string, string> AvailableLanguages = new()
    {
        { "English", "en" },
        { "Español", "es" },
        { "Français", "fr" },
        { "Deutsch", "de" },
        { "中文", "ch" },
        { "日本語", "jp" },
        { "عربي", "ar" }
    };

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the Traductor class for MAUI applications.
    /// </summary>
    /// <param name="fileReader">Function to read files from the app package.</param>
    public Traductor(Func<string, Task<Stream>> fileReader)
    {
        _fileReader = fileReader;
    }

    /// <summary>
    /// Initializes a new instance of the Traductor class for Web applications.
    /// Uses default file reading mechanism.
    /// </summary>
    public Traductor()
    {
        _fileReader = DefaultFileReader;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the translation for a given key.
    /// </summary>
    /// <param name="key">The translation key to look up.</param>
    /// <returns>The translated string, or the key itself if not found.</returns>
    public string GetTraduction(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (!_isInitialized)
            return key; // Return key during initialization to prevent "NOT FOUND" spam

        if (_languageKeys.TryGetValue(key, out var translation))
            return translation;

        // Return the key itself as fallback (useful for debugging missing translations)
        Console.WriteLine($"[Traductor] Missing translation key: {key} for language: {_lang}");
        return key;
    }

    /// <summary>
    /// Gets the translation for a given key with parameter substitution.
    /// </summary>
    /// <param name="key">The translation key to look up.</param>
    /// <param name="args">Arguments to substitute into the translation string.</param>
    /// <returns>The translated and formatted string.</returns>
    public string GetTraduction(string key, params object[] args)
    {
        var translation = GetTraduction(key);
        try
        {
            return string.Format(translation, args);
        }
        catch
        {
            return translation;
        }
    }

    /// <summary>
    /// Shorthand method for GetTraduction.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <returns>The translated string.</returns>
    public string T(string key) => GetTraduction(key);

    /// <summary>
    /// Shorthand method for GetTraduction with parameters.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <param name="args">Arguments for string formatting.</param>
    /// <returns>The translated and formatted string.</returns>
    public string T(string key, params object[] args) => GetTraduction(key, args);

    /// <summary>
    /// Initializes the translation service and loads the saved language preference.
    /// Should be called during application startup.
    /// </summary>
    /// <param name="savedLanguage">Optional saved language code from storage.</param>
    public async Task InitializeAsync(string? savedLanguage = null)
    {
        try
        {
            _lang = !string.IsNullOrEmpty(savedLanguage) ? savedLanguage : DEFAULT_LANGUAGE;

            // Validate the language code
            if (!AvailableLanguages.ContainsValue(_lang))
            {
                _lang = DEFAULT_LANGUAGE;
            }

            _languageKeys = await LoadTranslationsAsync(_lang);
            _isInitialized = true;
            OnLangChanged?.Invoke();
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Console.WriteLine($"[Traductor] Initialization error: {ex.Message}");
            _lang = DEFAULT_LANGUAGE;
            _languageKeys = new Dictionary<string, string>();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Changes the current language and reloads translations.
    /// </summary>
    /// <param name="languageCode">The new language code (e.g., "en", "es", "fr").</param>
    /// <returns>True if language was changed successfully, false otherwise.</returns>
    public async Task<bool> SetLanguageAsync(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
            return false;

        if (!AvailableLanguages.ContainsValue(languageCode))
        {
            LastError = $"Unsupported language code: {languageCode}";
            return false;
        }

        if (_lang == languageCode && _isInitialized)
            return true; // Already set

        try
        {
            _languageKeys = await LoadTranslationsAsync(languageCode);
            _lang = languageCode;
            OnLangChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Console.WriteLine($"[Traductor] Error setting language: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the display name for a language code.
    /// </summary>
    /// <param name="languageCode">The language code.</param>
    /// <returns>The display name of the language.</returns>
    public static string GetLanguageDisplayName(string languageCode)
    {
        return AvailableLanguages.FirstOrDefault(x => x.Value == languageCode).Key ?? "Unknown";
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads translations from a JSON dictionary file.
    /// </summary>
    /// <param name="languageCode">The language code to load.</param>
    /// <returns>Dictionary of translation keys and values.</returns>
    private async Task<Dictionary<string, string>> LoadTranslationsAsync(string languageCode)
    {
        try
        {
            // For MAUI, files are in Resources/Raw/Dictionaries/ - accessed without the Resources/Raw prefix
            var filePath = $"Dictionaries/{languageCode}.json";

            using var stream = await _fileReader(filePath);
            if (stream == null)
            {
                Console.WriteLine($"[Traductor] Dictionary file not found: {filePath}");
                return new Dictionary<string, string>();
            }

            var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);
            Console.WriteLine($"[Traductor] Loaded {dictionary?.Count ?? 0} translations for language: {languageCode}");
            return dictionary ?? new Dictionary<string, string>();
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"[Traductor] Dictionary file not found for language: {languageCode}");
            return new Dictionary<string, string>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[Traductor] JSON parsing error for language {languageCode}: {ex.Message}");
            return new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Traductor] Error loading translations for {languageCode}: {ex.Message}");
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Default file reader for web applications.
    /// </summary>
    private async Task<Stream> DefaultFileReader(string path)
    {
        var fullPath = Path.Combine("wwwroot", path);
        if (File.Exists(fullPath))
        {
            return File.OpenRead(fullPath);
        }
        throw new FileNotFoundException($"File not found: {fullPath}");
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the translation service.
    /// </summary>
    public void Dispose()
    {
        OnLangChanged = null;
    }

    #endregion
}
