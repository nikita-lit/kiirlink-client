using KiirLink.Services;

namespace KiirLink.Pages;

public partial class CreateAccountPage
{
    private readonly AuthService _authService;

    public CreateAccountPage( AuthService authService )
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

        CreateAccountButton.IsEnabled = true;
    }

    private async Task CreateAccountAsync()
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

        if ( string.IsNullOrWhiteSpace( email ) || string.IsNullOrWhiteSpace( password ) )
        {
            await DisplayAlertAsync( "Missing details", "Enter an email and password.", "OK" );
            return;
        }

        if ( password != confirmPassword )
        {
            await DisplayAlertAsync( "Passwords do not match", "Check the confirmation password and try again.", "OK" );
            return;
        }

        CreateAccountButton.IsEnabled = false;

        try
        {
            var register = await _authService.RegisterAsync( email, password );
            if ( !register.Success )
            {
                await DisplayAlertAsync( "Create account failed", register.Error ?? "Could not create the account.",
                    "OK" );
                return;
            }

            var login = await _authService.LoginAsync( email, password );
            if ( !login.Success )
            {
                await DisplayAlertAsync( "Account created",
                    "Your account was created, but automatic sign in failed. Please sign in.", "OK" );
                await Shell.Current.GoToAsync( "//SignIn" );
                return;
            }

            await DisplayAlertAsync( "Signed up", "Your account has been created and you're signed in.", "OK" );
            EmailEntry.Text = string.Empty;
            PasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
            await Shell.Current.GoToAsync( "//Home" );
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( "Error", ex.Message, "OK" );
        }
        finally
        {
            CreateAccountButton.IsEnabled = true;
        }
    }

    private async void OnCreateAccountClicked( object? sender, EventArgs e )
    {
        await CreateAccountAsync();
    }

    private async void OnBackToSignInClicked( object? sender, EventArgs e )
    {
        await Shell.Current.GoToAsync( "//SignIn" );
    }
}