using CommunityToolkit.Maui.Extensions;
using KiirLink.Controls;
using KiirLink.Services;
using KiirLink.ViewModels;

namespace KiirLink.Pages;

public partial class ProfilePage
{
    private readonly ProfileViewModel _viewModel;
    private readonly ApiClient _api;
    private readonly LinkPreferencesService _linkPreferences;
    private bool _syncingThemeSwitch;
    private readonly EventHandler _themeChangedHandler;

    public ProfilePage(ProfileViewModel viewModel, ApiClient api,
        LinkPreferencesService linkPreferences)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _api = api;
        _linkPreferences = linkPreferences;
        BindingContext = viewModel;
        _themeChangedHandler = (_, _) => ApplyThemeToIcons();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.ThemeChanged += _themeChangedHandler;
        SyncThemeSwitch();
        SyncLinkPreferences();
        ApplyThemeToIcons();
        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        ThemeService.ThemeChanged -= _themeChangedHandler;
        base.OnDisappearing();
    }

    private void SyncThemeSwitch()
    {
        _syncingThemeSwitch = true;
        ThemeSwitch.IsToggled = ThemeService.IsDark;
        _syncingThemeSwitch = false;
    }

    private void SyncLinkPreferences()
    {
        DefaultCategoryLabel.Text = _linkPreferences.DefaultCategoryName;
        AutoExpirationLabel.Text = _linkPreferences.AutoExpirationDisplay;
        PrivacyLabel.Text = _linkPreferences.PrivacyDisplay;
        LanguageLabel.Text = L(
            LocalizationManager.Instance.CurrentLanguage switch
            {
                "ru" => "Russian",
                "et" => "Estonian",
                _ => "English"
            });
    }

    private async void OnLanguageTapped(object? sender, TappedEventArgs e)
    {
        var choices = new Dictionary<string, string>
        {
            [L("English")] = "en",
            [L("Russian")] = "ru",
            [L("Estonian")] = "et"
        };

        var selection = await DisplayActionSheetAsync(
            L("SelectLanguage"),
            L("Cancel"),
            null,
            choices.Keys.ToArray());

        if (selection is not null && choices.TryGetValue(selection, out var languageCode))
        {
            LocalizationManager.Instance.SetCulture(languageCode);
            SyncLinkPreferences();
        }
    }

    private async void OnDefaultCategoryTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var categories = await _api.GetCategoriesAsync();
            var none = L("None");
            var cancel = L("Cancel");
            var options = new[] { none }.Concat(categories.Select(category => category.Name)).ToArray();
            var selection = await DisplayActionSheetAsync(L("DefaultCategory"), cancel, null, options);
            if (string.IsNullOrWhiteSpace(selection) || selection == cancel)
                return;

            var category = categories.FirstOrDefault(item =>
                string.Equals(item.Name, selection, StringComparison.Ordinal));
            _linkPreferences.SetDefaultCategory(category?.Id, category?.Name);
            SyncLinkPreferences();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(L("Error"), F("CouldNotLoadCategoriesProfile", ex.Message), "OK");
        }
    }

    private async void OnAutoExpirationTapped(object? sender, TappedEventArgs e)
    {
        var result = await this.ShowPopupAsync<ExpirationDateSelection>(
            new ExpirationDatePopup(_linkPreferences.AutoExpirationDate),
            UiHelpers.PlainPopup());
        if (result.WasDismissedByTappingOutsideOfPopup || result.Result is null)
            return;

        var selection = result.Result;
        if (!selection.IsNever && !selection.Date.HasValue)
            return;

        _linkPreferences.SetAutoExpiration(selection.IsNever ? null : selection.Date);
        SyncLinkPreferences();
    }

    private async void OnPrivacyTapped(object? sender, TappedEventArgs e)
    {
        var selection = await DisplayActionSheetAsync(
            L("DefaultPrivacy"),
            L("Cancel"),
            null,
            L("Public"),
            L("Private"));
        if (selection is null)
            return;

        _linkPreferences.SetPrivacy(selection == L("Public"));
        SyncLinkPreferences();
    }

    private async void OnEditProfileClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Profile", "Profile editing is ready to connect to your account data.", "OK");
    }

    private async void OnChangePasswordTapped(object? sender, TappedEventArgs e)
    {
        var result = await this.ShowPopupAsync<bool>(
            new ChangePasswordPopup(_api),
            UiHelpers.PlainPopup());
        
        if (!result.WasDismissedByTappingOutsideOfPopup && result.Result)
            await DisplayAlertAsync(
                L("PasswordChanged"),
                L("PasswordChangedMessage"),
                "OK");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var shouldLogout = await DisplayAlertAsync(
            L("LogOutTitle"),
            L("LogOutConfirmation"),
            L("LogOutTitle"),
            L("Cancel"));

        if (shouldLogout)
            await _viewModel.LogoutAsync();
    }

    private void OnThemeToggled(object? sender, ToggledEventArgs e)
    {
        if (_syncingThemeSwitch)
            return;

        ThemeService.SetDark(e.Value);

        var background = (Color)Application.Current!.Resources["AppBackground"];
        BackgroundColor = background;
        PageLayout.BackgroundColor = background;
        ApplyThemeToIcons();
    }

    private void ApplyThemeToIcons()
    {
        var tint = (Color)Application.Current!.Resources["AppText"];
        foreach (var icon in new[]
                 {
                     UserProfileIcon, MailIcon, ChangePasswordIcon, DarkThemeIcon,
                     LanguageIcon, FolderIcon, ClockIcon, PrivacyIcon
                 })
            icon.SetTint(tint);
    }

    private static string L(string key) => LocalizationManager.Instance.Get(key);
    private static string F(string key, params object[] args) => LocalizationManager.Instance.Format(key, args);
}
