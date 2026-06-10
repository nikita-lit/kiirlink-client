using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using KiirLink.Controls;
using KiirLink.Services;
using KiirLink.ViewModels;

namespace KiirLink.Pages;

public partial class ProfilePage
{
    private readonly ProfileViewModel _viewModel;
    private readonly IAuthService _authService;
    private readonly ILinkService _linkService;
    private readonly LinkPreferencesService _linkPreferences;
    private bool _syncingThemeSwitch;
    private readonly EventHandler _themeChangedHandler;

    public ProfilePage(ProfileViewModel viewModel, IAuthService authService, ILinkService linkService,
        LinkPreferencesService linkPreferences)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService;
        _linkService = linkService;
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
        LanguageLabel.Text = LocalizationManager.Instance.Get(
            LocalizationManager.Instance.CurrentLanguage switch
            {
                "ru" => "Russian",
                "et" => "Estonian",
                _ => "English"
            });
    }

    private async void OnLanguageTapped(object? sender, TappedEventArgs e)
    {
        var localization = LocalizationManager.Instance;
        var choices = new Dictionary<string, string>
        {
            [localization.Get("English")] = "en",
            [localization.Get("Russian")] = "ru",
            [localization.Get("Estonian")] = "et"
        };

        var selection = await DisplayActionSheetAsync(
            localization.Get("SelectLanguage"),
            localization.Get("Cancel"),
            null,
            choices.Keys.ToArray());

        if (selection is not null && choices.TryGetValue(selection, out var languageCode))
        {
            localization.SetCulture(languageCode);
            SyncLinkPreferences();
        }
    }

    private async void OnDefaultCategoryTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var categories = await _linkService.GetCategoriesAsync();
            var none = LocalizationManager.Instance.Get("None");
            var cancel = LocalizationManager.Instance.Get("Cancel");
            var options = new[] { none }.Concat(categories.Select(category => category.Name)).ToArray();
            var selection = await DisplayActionSheetAsync(
                LocalizationManager.Instance.Get("DefaultCategory"), cancel, null, options);
            if (string.IsNullOrWhiteSpace(selection) || selection == cancel)
                return;

            var category = categories.FirstOrDefault(item =>
                string.Equals(item.Name, selection, StringComparison.Ordinal));
            _linkPreferences.SetDefaultCategory(category?.Id, category?.Name);
            SyncLinkPreferences();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(LocalizationManager.Instance.Get("Error"),
                LocalizationManager.Instance.Format("CouldNotLoadCategoriesProfile", ex.Message), "OK");
        }
    }

    private async void OnAutoExpirationTapped(object? sender, TappedEventArgs e)
    {
        var result = await this.ShowPopupAsync<ExpirationDateSelection>(
            new ExpirationDatePopup(_linkPreferences.AutoExpirationDate),
            new PopupOptions
            {
                Shape = null,
                Shadow = null
            });
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
            LocalizationManager.Instance.Get("DefaultPrivacy"),
            LocalizationManager.Instance.Get("Cancel"),
            null,
            LocalizationManager.Instance.Get("Public"),
            LocalizationManager.Instance.Get("Private"));
        if (selection is null)
            return;

        _linkPreferences.SetPrivacy(selection == LocalizationManager.Instance.Get("Public"));
        SyncLinkPreferences();
    }

    private async void OnEditProfileClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Profile", "Profile editing is ready to connect to your account data.", "OK");
    }

    private async void OnChangePasswordTapped(object? sender, TappedEventArgs e)
    {
        var result = await this.ShowPopupAsync<bool>(
            new ChangePasswordPopup(_authService), 
            new PopupOptions
        {
            Shape = null,
            Shadow = null,
        } );
        
        if (!result.WasDismissedByTappingOutsideOfPopup && result.Result)
            await DisplayAlertAsync(
                LocalizationManager.Instance.Get("PasswordChanged"),
                LocalizationManager.Instance.Get("PasswordChangedMessage"),
                "OK");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var shouldLogout =
            await DisplayAlertAsync(
                LocalizationManager.Instance.Get("LogOutTitle"),
                LocalizationManager.Instance.Get("LogOutConfirmation"),
                LocalizationManager.Instance.Get("LogOutTitle"),
                LocalizationManager.Instance.Get("Cancel"));

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
        SetIconTint(UserProfileIcon, tint);
        SetIconTint(MailIcon, tint);
        SetIconTint(ChangePasswordIcon, tint);
        SetIconTint(DarkThemeIcon, tint);
        SetIconTint(LanguageIcon, tint);
        SetIconTint(FolderIcon, tint);
        SetIconTint(ClockIcon, tint);
        SetIconTint(PrivacyIcon, tint);
    }

    private static void SetIconTint(Image image, Color tint)
    {
        var behavior = image.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.IconTintColorBehavior>().FirstOrDefault();
        if (behavior is null)
        {
            behavior = new CommunityToolkit.Maui.Behaviors.IconTintColorBehavior();
            image.Behaviors.Add(behavior);
        }

        behavior.TintColor = tint;
    }
}
