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
    private bool _syncingThemeSwitch;
    private readonly EventHandler _themeChangedHandler;

    public ProfilePage(ProfileViewModel viewModel, IAuthService authService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService;
        BindingContext = viewModel;
        _themeChangedHandler = (_, _) => ApplyThemeToIcons();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.ThemeChanged += _themeChangedHandler;
        SyncThemeSwitch();
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
            await DisplayAlertAsync("Password changed", "Your password has been updated.", "OK");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var shouldLogout =
            await DisplayAlertAsync("Log out", "Are you sure you want to log out?", "Log out", "Cancel");

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
