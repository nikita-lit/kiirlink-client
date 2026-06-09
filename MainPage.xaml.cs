using KiirLink.Services;

namespace KiirLink;

public partial class MainPage : ContentPage
{
    private readonly LinkService _linkService;

    public MainPage( LinkService linkService )
    {
        InitializeComponent();
        _linkService = linkService;
    }

    private async void OnContinueClicked( object? sender, EventArgs e )
    {
        var slug = CustomLinkEntry.Text?.Trim();

        if ( string.IsNullOrWhiteSpace( slug ) )
        {
            await DisplayAlertAsync( "Add a custom link", "Enter the short name you want to use.", "OK" );
            return;
        }

        // Build full URL from slug (treat it as the original URL if it starts with http,
        // otherwise prefix with https:// so the server can validate it)
        var originalUrl = slug.StartsWith( "http", StringComparison.OrdinalIgnoreCase )
            ? slug
            : $"https://{slug}";

        ContinueButton.IsEnabled = false;
        ContinueButton.Text = "Creating…";

        try
        {
            var result = await _linkService.ShortenLinkAsync( originalUrl, isPublic: true );

            if ( result.Success )
            {
                CustomLinkEntry.Text = string.Empty;
                await DisplayAlertAsync( "Link created!", $"Your short link is ready.", "Done" );
                await Shell.Current.GoToAsync( "//Links" );
            }
            else if ( result.Error?.Contains( "HTTP 401" ) == true )
            {
                await DisplayAlertAsync( "Sign in required", "Please sign in before creating links.", "OK" );
            }
            else
            {
                await DisplayAlertAsync( "Error", $"Could not create the link. Please try again. ({result.Error})",
                    "OK" );
            }
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( "Network error", ex.Message, "OK" );
        }
        finally
        {
            ContinueButton.IsEnabled = true;
            ContinueButton.Text = "CONTINUE";
        }
    }
}