using KiirLink.Services;

namespace KiirLink;

public partial class AppShell : Shell
{
    private readonly IAuthService _auth;
    private bool _redirectingToSignIn;

    public AppShell(IAuthService auth)
    {
        InitializeComponent();
        _auth = auth;
        Navigating += OnNavigating;
    }

    private async void OnNavigating( object? sender, ShellNavigatingEventArgs e )
    {
        var target = e.Target.Location.OriginalString;
        if ( target.Contains( "SignIn", StringComparison.OrdinalIgnoreCase ) ||
             target.Contains( "CreateAccount", StringComparison.OrdinalIgnoreCase ) )
            return;

        if (await _auth.IsAuthenticatedAsync() || _redirectingToSignIn)
            return;

        _redirectingToSignIn = true;
        e.Cancel();

        Dispatcher.Dispatch( async () =>
        {
            try
            {
                await GoToAsync( "//SignIn" );
            }
            finally
            {
                _redirectingToSignIn = false;
            }
        } );
    }
}
