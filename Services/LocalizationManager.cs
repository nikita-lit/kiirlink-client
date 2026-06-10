using System.ComponentModel;
using System.Globalization;
using KiirLink.Resources.Strings;

namespace KiirLink.Services;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private const string LanguagePreferenceKey = "app_language";
    private static readonly HashSet<string> SupportedLanguages = ["en", "ru", "et"];

    public static LocalizationManager Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CultureChanged;

    public string this[string key] => AppResources.Get(key, CurrentCulture);
    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo("en");
    public string CurrentLanguage => CurrentCulture.TwoLetterISOLanguageName;

    public void Initialize()
    {
        var savedLanguage = Preferences.Default.Get(LanguagePreferenceKey, string.Empty);
        var deviceLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        SetCulture(SupportedLanguages.Contains(savedLanguage) ? savedLanguage :
            SupportedLanguages.Contains(deviceLanguage) ? deviceLanguage : "en", false);
    }

    public void SetCulture(string languageCode) => SetCulture(languageCode, true);

    public string Get(string key) => this[key];

    public string Format(string key, params object[] args) =>
        string.Format(CurrentCulture, this[key], args);

    private void SetCulture(string languageCode, bool save)
    {
        if (!SupportedLanguages.Contains(languageCode))
            languageCode = "en";

        CurrentCulture = CultureInfo.GetCultureInfo(languageCode switch
        {
            "ru" => "ru-RU",
            "et" => "et-EE",
            _ => "en-US"
        });
        CultureInfo.CurrentCulture = CurrentCulture;
        CultureInfo.CurrentUICulture = CurrentCulture;
        CultureInfo.DefaultThreadCurrentCulture = CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CurrentCulture;

        if (save)
            Preferences.Default.Set(LanguagePreferenceKey, languageCode);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    public string LocalizeAuthError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Get("AuthenticationFailed");

        return string.Join(
            Environment.NewLine,
            message.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => AuthErrorKeys.TryGetValue(line.Trim(), out var key) ? Get(key) : line));
    }

    private static readonly IReadOnlyDictionary<string, string> AuthErrorKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Too many attempts. Wait a moment and try again."] = "TooManyAttempts",
            ["Incorrect email or password."] = "IncorrectEmailPassword",
            ["Your session has expired. Sign in again and retry."] = "SessionExpired",
            ["An account with this email already exists."] = "DuplicateEmail",
            ["Enter a valid email address."] = "InvalidEmail",
            ["The password is too short."] = "PasswordTooShort",
            ["The password must contain a number."] = "PasswordRequiresDigit",
            ["The password must contain a lowercase letter."] = "PasswordRequiresLower",
            ["The password must contain an uppercase letter."] = "PasswordRequiresUpper",
            ["The password must contain a special character."] = "PasswordRequiresNonAlphanumeric",
            ["The password must contain more unique characters."] = "PasswordRequiresUniqueChars",
            ["The current password is incorrect."] = "CurrentPasswordIncorrect",
            ["Could not create the account. Check the email and password."] = "RegisterAuthFailed",
            ["Could not change the password. Check your current and new passwords."] = "ChangePasswordAuthFailed",
            ["Authentication failed. Please try again."] = "AuthenticationFailed",
            ["Invalid response from server."] = "InvalidServerResponse"
        };
}
