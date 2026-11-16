using Microsoft.Extensions.Logging;
using System.Text.Json;
using Kmila.Shared.Data;
using Kmila.Shared.Models;

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
/// <b>Reactive Architecture:</b>
/// The service uses a reactive pattern for instant language switching:
/// <list type="number">
/// <item><description>LanguageChanger saves language to DB via <c>ApplicationDbContext.SetSettingAsync</c></description></item>
/// <item><description>ApplicationDbContext fires <c>StaticOnValueChanged</c> event</description></item>
/// <item><description>Traductor catches the event, reads new language from storage, reloads translations</description></item>
/// <item><description>Traductor fires <c>StaticOnLangChanged</c> event</description></item>
/// <item><description>MainLayout catches event and calls <c>StateHasChanged()</c> to refresh UI</description></item>
/// </list>
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
    /// Gets a read-only view of the currently loaded translation dictionary
    /// (every key/value for the active language). Used by the settings page to
    /// export the live dictionary so it can be edited and re-imported.
    /// </summary>
    public IReadOnlyDictionary<string, string> CurrentEntries => _languageKeys;

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
    /// Static event raised when the language changes. Components should subscribe to this
    /// and call StateHasChanged() when notified. Using static ensures global propagation.
    /// </summary>
    public static event Action? StaticOnLangChanged;

    /// <summary>
    /// Instance event raised when the language changes (kept for backwards compatibility).
    /// Prefer using StaticOnLangChanged for reliable global notifications.
    /// </summary>
    public event Action? OnLangChanged;

    /// <summary>
    /// Invokes both static and instance language change events.
    /// </summary>
    private void NotifyLanguageChanged()
    {
        _logger.LogInformation("[Traductor] Notifying language change to all subscribers...");
        StaticOnLangChanged?.Invoke();
        OnLangChanged?.Invoke();
    }

    #endregion

    #region Private Fields

    private Dictionary<string, string> _languageKeys = new();
    private string _lang = DEFAULT_LANGUAGE;
    private bool _isInitialized;
    private readonly Func<string, Task<Stream>> _fileReader;
    private readonly CancellationTokenSource _cts = new();
    private Func<string, Task<string?>>? _settingsReader;

    /// <summary>
    /// Available languages with their display names (display → code). Mutable
    /// at runtime so <see cref="AddUserLanguage"/> can merge in user-imported
    /// packs discovered via <see cref="UserAssetStore"/>.
    /// </summary>
    public static Dictionary<string, string> AvailableLanguages { get; } = new()
    {
        { "English", "en" },
        { "Español", "es" },
        { "Français", "fr" },
        { "Deutsch", "de" },
        { "中文", "ch" },
        { "日本語", "jp" },
        { "عربي", "ar" }
    };

    /// <summary>
    /// Register a user-provided language pack so it shows up in the language
    /// picker and <see cref="LoadTranslationsAsync"/> can resolve it. No-op if
    /// the code already exists (bundled languages always win).
    /// </summary>
    /// <param name="displayName">Name shown in the picker (e.g. "Klingon").</param>
    /// <param name="code">Short code used in file names and settings (e.g. "klg").</param>
    public static void AddUserLanguage(string displayName, string code)
    {
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(code)) return;
        if (AvailableLanguages.ContainsValue(code)) return; // bundled or already added
        AvailableLanguages[displayName] = code;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Logger — flows to both the console AND the in-app Logs page via the
    /// registered <see cref="InMemoryLoggerProvider"/>. Never null; callers that
    /// don't pass a logger get a no-op instance.
    /// </summary>
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the Traductor class for MAUI applications.
    /// </summary>
    /// <param name="fileReader">Function to read files from the app package.</param>
    /// <param name="logger">Optional logger (NullLogger fallback when omitted).</param>
    public Traductor(Func<string, Task<Stream>> fileReader, Microsoft.Extensions.Logging.ILogger<Traductor>? logger = null)
    {
        _fileReader = fileReader;
        _logger = (Microsoft.Extensions.Logging.ILogger?)logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Traductor>.Instance;
        ApplicationDbContext.StaticOnValueChanged += OnValueChanged;
    }

    /// <summary>
    /// Initializes a new instance of the Traductor class for Web applications.
    /// Uses default file reading mechanism.
    /// </summary>
    public Traductor()
    {
        _fileReader = DefaultFileReader;
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<Traductor>.Instance;
        ApplicationDbContext.StaticOnValueChanged += OnValueChanged;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles configuration value changes from the database.
    /// Triggers a language reload if the language setting changes.
    /// </summary>
    /// <param name="key">The configuration key that changed.</param>
    private void OnValueChanged(string key)
    {
        if (key == AppSettings.KEY_LANGUAGE)
        {
            _logger.LogInformation($"[Traductor] Language configuration changed via storage event");
            Task.Run(async () => await ReloadLanguageFromStorageAsync(), _cts.Token);
        }
    }

    /// <summary>
    /// Reloads the language from storage when notified of a change.
    /// This is called reactively when ApplicationDbContext fires StaticOnValueChanged.
    /// </summary>
    private async Task ReloadLanguageFromStorageAsync()
    {
        try
        {
            // Read the new language value from storage
            if (_settingsReader != null)
            {
                var newLang = await _settingsReader(AppSettings.KEY_LANGUAGE);
                if (!string.IsNullOrEmpty(newLang) && AvailableLanguages.ContainsValue(newLang))
                {
                    _lang = newLang;
                }
            }

            _languageKeys = await LoadTranslationsAsync(_lang);
            _logger.LogInformation($"[Traductor] Language reloaded: {_lang}");
            NotifyLanguageChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Traductor] Error reloading language: {ex.Message}");
        }
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
        _logger.LogWarning($"[Traductor] Missing translation key: {key} for language: {_lang}");
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
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, translation, args);
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
    /// <param name="settingsReader">Optional callback to read settings from storage (for reactive updates).</param>
    public async Task InitializeAsync(string? savedLanguage = null, Func<string, Task<string?>>? settingsReader = null)
    {
        _logger.LogInformation($"[Traductor] Initializing with savedLanguage: {savedLanguage ?? "(null)"}");

        _settingsReader = settingsReader;
        _lang = !string.IsNullOrEmpty(savedLanguage) ? savedLanguage : DEFAULT_LANGUAGE;

        // Validate the language code
        if (!AvailableLanguages.ContainsValue(_lang))
        {
            _logger.LogWarning($"[Traductor] Invalid language '{_lang}', falling back to {DEFAULT_LANGUAGE}");
            _lang = DEFAULT_LANGUAGE;
        }

        try
        {
            _languageKeys = await LoadTranslationsAsync(_lang);
            _isInitialized = true;
            _logger.LogInformation($"[Traductor] Successfully initialized with {_languageKeys.Count} keys for language: {_lang}");
            NotifyLanguageChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Traductor] Failed to load '{_lang}': {ex.Message}");

            // If not already trying default language, try it as fallback
            if (_lang != DEFAULT_LANGUAGE)
            {
                _logger.LogWarning($"[Traductor] Trying fallback language: {DEFAULT_LANGUAGE}");
                try
                {
                    _languageKeys = await LoadTranslationsAsync(DEFAULT_LANGUAGE);
                    _lang = DEFAULT_LANGUAGE;
                    _isInitialized = true;
                    _logger.LogWarning($"[Traductor] Fallback successful with {_languageKeys.Count} keys");
                    NotifyLanguageChanged();
                    return;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError($"[Traductor] Fallback also failed: {fallbackEx.Message}");
                }
            }

            // Complete failure - initialize with empty dictionary
            LastError = ex.Message;
            _lang = DEFAULT_LANGUAGE;
            _languageKeys = new Dictionary<string, string>();
            _isInitialized = true;
            _logger.LogError($"[Traductor] Initialization failed completely. Running without translations.");
            NotifyLanguageChanged();
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
            _logger.LogInformation($"[Traductor] Setting language to: {languageCode}");
            _languageKeys = await LoadTranslationsAsync(languageCode);
            _lang = languageCode;
            _logger.LogInformation($"[Traductor] Language set successfully, notifying subscribers...");
            NotifyLanguageChanged();
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError($"[Traductor] Error setting language: {ex.Message}");
            return false;
        }
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
        // For MAUI, files are in Resources/Raw/Dictionaries/ - accessed without the Resources/Raw prefix
        // For Web, the file reader handles path resolution
        var filePath = $"Dictionaries/{languageCode}.json";
        _logger.LogInformation($"[Traductor] Attempting to load: {filePath}");

        try
        {
            using var stream = await _fileReader(filePath);
            if (stream == null)
            {
                _logger.LogWarning($"[Traductor] File reader returned null for: {filePath}");
                throw new FileNotFoundException($"Dictionary file returned null: {filePath}");
            }

            var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);

            if (dictionary == null || dictionary.Count == 0)
            {
                _logger.LogWarning($"[Traductor] Dictionary was empty or null for language: {languageCode}");
                throw new InvalidOperationException($"Dictionary was empty for language: {languageCode}");
            }

            _logger.LogInformation($"[Traductor] Successfully loaded {dictionary.Count} translations for language: {languageCode}");
            return dictionary;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogInformation($"[Traductor] File not found for language {languageCode}: {ex.Message}");
            throw; // Re-throw to propagate to InitializeAsync for proper error handling
        }
        catch (JsonException ex)
        {
            _logger.LogError($"[Traductor] JSON parsing error for language {languageCode}: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Traductor] Error loading translations for {languageCode}: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Default file reader for web applications.
    /// </summary>
    private async Task<Stream> DefaultFileReader(string path)
    {
        await Task.CompletedTask;
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
    /// Disposes the translation service and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        ApplicationDbContext.StaticOnValueChanged -= OnValueChanged;
        _cts?.Cancel();
        _cts?.Dispose();
        OnLangChanged = null;
        GC.SuppressFinalize(this);
    }

    #endregion
}
