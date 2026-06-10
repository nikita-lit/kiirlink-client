using System.Collections.ObjectModel;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Pages;

public partial class FavouritesPage
{
    private readonly LinkService _linkService;
    private readonly IConnectivityService _connectivity;
    private readonly EventHandler<bool> _connectivityChangedHandler;

    public ObservableCollection<LinkModel> Favourites { get; } = [];

    public FavouritesPage(LinkService linkService, IConnectivityService connectivity)
    {
        InitializeComponent();
        _linkService = linkService;
        _connectivity = connectivity;
        _connectivityChangedHandler = (_, online) =>
            Dispatcher.Dispatch(() => UpdateConnectivityState(online));
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _connectivity.ConnectivityChanged += _connectivityChangedHandler;
        UpdateConnectivityState(_connectivity.IsOnline);
        if (_connectivity.IsOnline)
            await LoadFavouritesAsync();
    }

    protected override void OnDisappearing()
    {
        _connectivity.ConnectivityChanged -= _connectivityChangedHandler;
        base.OnDisappearing();
    }

    private async Task LoadFavouritesAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        try
        {
            var favourites = await _linkService.GetFavouritesAsync();

            Favourites.Clear();
            foreach ( var link in favourites ) Favourites.Add( link );

            FavCountLabel.Text = favourites.Count.ToString();
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( "Error", $"Could not load favourites: {ex.Message}", "OK" );
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async Task NavigateToAnalyticsAsync( LinkModel? link )
    {
        if ( link is null ) return;

        AnalyticsPage.SelectedLinkId = link.ResolvedId;
        AnalyticsPage.SelectedLink = link;
        await Shell.Current.GoToAsync( "//Analytics" );
    }

    private async void OnLinkCardTapped( object? sender, TappedEventArgs e )
    {
        if ( sender is not Controls.LinkCard card ) return;

        await NavigateToAnalyticsAsync( Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId ) );
    }

    private async void OnLinkAnalyticsRequested( object? sender, EventArgs e )
    {
        if ( sender is not Controls.LinkCard card ) return;

        await NavigateToAnalyticsAsync( Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId ) );
    }

    private async void OnLinkCategoryRequested( object? sender, EventArgs e )
    {
        if ( sender is not Controls.LinkCard card ) return;

        var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
        if ( link is null ) return;

        var category = await PromptAssignExistingCategoryAsync( link.ResolvedId );
        if ( category is null )
            return;

        link.CategoryId = category.Id;
        link.CategoryName = category.Name;
        await LoadFavouritesAsync();
    }

    private async void OnLinkCategoryDeleteRequested( object? sender, EventArgs e )
    {
        if ( sender is not Controls.LinkCard card ) return;

        var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
        if ( link is null || link.CategoryId is null )
        {
            await DisplayAlertAsync( "No category", "This link does not have a category to delete.", "OK" );
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Delete category",
            $"Delete '{link.CategoryName}'? This removes the category from the server.",
            "Delete",
            "Cancel" );

        if ( !confirm )
            return;

        var success = await _linkService.DeleteCategoryAsync( link.CategoryId.Value );
        if ( !success )
        {
            await DisplayAlertAsync( "Error", "Could not delete the category.", "OK" );
            return;
        }

        link.CategoryId = null;
        link.CategoryName = null;
        await DisplayAlertAsync( "Deleted", "Category has been deleted.", "OK" );
        await LoadFavouritesAsync();
    }

    private async void OnLinkFavouriteToggleRequested( object? sender, EventArgs e )
    {
        if ( sender is not Controls.LinkCard card ) return;

        try
        {
            var success = await _linkService.RemoveFavouriteAsync( card.LinkId );
            if ( success )
            {
                var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
                if ( link is not null )
                    Favourites.Remove( link );
                FavCountLabel.Text = Favourites.Count.ToString();
                await DisplayAlertAsync( "Removed", "Removed from favorites.", "OK" );
            }
            else
            {
                await DisplayAlertAsync( "Error", "Could not update favourite status", "OK" );
            }
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( "Error", $"Error: {ex.Message}", "OK" );
        }
    }

    private async void OnLinkDeleteRequested( object? sender, EventArgs e )
    {
        if ( sender is not Controls.LinkCard card ) return;

        var confirm = await DisplayAlertAsync(
            "Delete link",
            $"Delete {card.Title}? This cannot be undone.",
            "Delete",
            "Cancel" );

        if ( !confirm ) return;

        try
        {
            var success = await _linkService.RemoveLinkAsync( card.LinkId );
            if ( success )
            {
                var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
                if ( link is not null )
                    Favourites.Remove( link );
                FavCountLabel.Text = Favourites.Count.ToString();
                await DisplayAlertAsync( "Deleted", "Link has been deleted.", "OK" );
            }
            else
            {
                await DisplayAlertAsync( "Error", "Could not delete link", "OK" );
            }
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( "Error", $"Error: {ex.Message}", "OK" );
        }
    }

    private async Task<CategoryModel?> PromptAssignExistingCategoryAsync( int linkId )
    {
        var categories = await _linkService.GetCategoriesAsync();
        if ( categories.Count == 0 )
        {
            await DisplayAlertAsync( "No categories", "Create a category first from Links > Categories.", "OK" );
            return null;
        }

        var action = await DisplayActionSheetAsync( "Assign category", "Cancel", null,
            categories.Select( c => c.Name ).ToArray() );
        if ( string.IsNullOrWhiteSpace( action ) || action == "Cancel" )
            return null;

        var category =
            categories.FirstOrDefault( c => string.Equals( c.Name, action, StringComparison.OrdinalIgnoreCase ) );
        if ( category is null )
            return null;

        var assigned = await _linkService.AssignCategoryAsync( linkId, category.Id );
        if ( !assigned )
        {
            await DisplayAlertAsync( "Error", "Could not assign the category to the link.", "OK" );
            return null;
        }

        await DisplayAlertAsync( "Category assigned", $"'{category.Name}' is now attached to the link.", "OK" );
        return category;
    }

    private void UpdateConnectivityState(bool online)
    {
        OfflineBanner.IsVisible = !online;
        FavouritesCollection.IsEnabled = online;
    }
}
