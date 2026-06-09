using KiirLink.Services;

namespace KiirLink.Pages;

public partial class ProfilePage
{
    private readonly AuthService _authService;

    public ProfilePage( AuthService authService )
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            var info = await _authService.GetProfileAsync();
            if ( info is null ) return;

            // Populate labels with real data
            ProfileEmailLabel.Text = info.Email;
            ProfileNameLabel.Text = info.Email.Split( '@' )[0]; // use email prefix as display name
            AccountUsernameLabel.Text = info.Email.Split( '@' )[0];
            AccountEmailLabel.Text = info.Email;
        }
        catch
        {
            // non-critical — keep the placeholder text
        }
    }

    private async void OnEditProfileClicked( object? sender, EventArgs e )
    {
        await DisplayAlertAsync( "Profile", "Profile editing is ready to connect to your account data.", "OK" );
    }

    private async void OnLogoutClicked( object? sender, EventArgs e )
    {
        var shouldLogout =
            await DisplayAlertAsync( "Log out", "Are you sure you want to log out?", "Log out", "Cancel" );

        if ( shouldLogout )
        {
            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync( "//SignIn" );
        }
    }
}