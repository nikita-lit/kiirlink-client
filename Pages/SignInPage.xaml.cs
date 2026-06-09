using KiirLink.Services;

namespace KiirLink.Pages;

public partial class SignInPage
{
    private readonly AuthService _authService;

    public SignInPage( AuthService authService )
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if ( await _authService.IsAuthenticatedAsync() )
        {
            await Shell.Current.GoToAsync( "//Home" );
            return;
        }

        SignInButton.IsEnabled = true;
    }

    private async Task SignInAsync()
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text ?? string.Empty;

        if ( string.IsNullOrWhiteSpace( email ) || string.IsNullOrWhiteSpace( password ) )
        {
            await DisplayAlertAsync( "Missing details", "Enter an email and password.", "OK" );
            return;
        }

        SignInButton.IsEnabled = false;

        try
        {
            var login = await _authService.LoginAsync( email, password );
            if ( !login.Success )
            {
                await DisplayAlertAsync( "Sign in failed", login.Error ?? "Could not sign in.", "OK" );
                return;
            }

            EmailEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            await Shell.Current.GoToAsync( "//Home" );
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( "Error", ex.Message, "OK" );
        }
        finally
        {
            SignInButton.IsEnabled = true;
        }
    }

    private async void OnSignInClicked( object? sender, EventArgs e )
    {
        await SignInAsync();
    }

    private async void OnCreateAccountClicked( object? sender, EventArgs e )
    {
        await Shell.Current.GoToAsync( "//CreateAccount" );
    }
}