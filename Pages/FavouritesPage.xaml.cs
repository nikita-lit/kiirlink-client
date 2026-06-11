using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Extensions;
using KiirLink.Controls;
using KiirLink.Extensions;
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

            Favourites.ReplaceWith(favourites);

            FavCountLabel.Text = favourites.Count.ToString();
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( L("Error"), F("CouldNotLoadFavourites", ex.Message), "OK" );
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
        if ( sender is not LinkCard card ) 
            return;

        await NavigateToAnalyticsAsync( Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId ) );
    }

    private async void OnLinkAnalyticsRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) 
            return;

        await NavigateToAnalyticsAsync( Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId ) );
    }

    private async void OnLinkCategoryRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) 
            return;

        var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
        if ( link is null ) return;

        var category = await UiHelpers.AssignCategoryAsync(
            this,
            _linkService,
            link.ResolvedId,
            await _linkService.GetCategoriesAsync());
        if ( category is null )
            return;

        link.CategoryId = category.Id;
        link.CategoryName = category.Name;
        await LoadFavouritesAsync();
    }
    
    private async void OnLinkFavouriteToggleRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) return;

        try
        {
            var success = await _linkService.RemoveFavouriteAsync( card.LinkId );
            if ( success )
            {
                var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
                if ( link is not null )
                    Favourites.Remove( link );
                FavCountLabel.Text = Favourites.Count.ToString();
                await DisplayAlertAsync( L("Removed"), L("RemovedFromFavourites"), "OK" );
            }
            else
            {
                await DisplayAlertAsync( L("Error"), L("CouldNotUpdateFavourite"), "OK" );
            }
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( L("Error"), F("ErrorDetails", ex.Message), "OK" );
        }
    }

    private async void OnLinkDeleteRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) 
            return;

        var confirm = await DisplayAlertAsync(
            L("DeleteLink"),
            F("DeleteLinkConfirmation", card.Title),
            L("Delete"),
            L("Cancel") );

        if ( !confirm ) 
            return;

        try
        {
            var success = await _linkService.RemoveLinkAsync( card.LinkId );
            if ( success )
            {
                var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
                if ( link is not null )
                    Favourites.Remove( link );
                FavCountLabel.Text = Favourites.Count.ToString();
                await DisplayAlertAsync( L("Deleted"), L("LinkDeleted"), "OK" );
            }
            else
            {
                await DisplayAlertAsync( L("Error"), L("CouldNotDeleteLink"), "OK" );
            }
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( L("Error"), F("ErrorDetails", ex.Message), "OK" );
        }
    }

    private async void OnCreateQRCodeClicked( object? sender, EventArgs e )
    {
        var page = Shell.Current?.CurrentPage;
        if ( page is null )
            return;
        
        if ( sender is not LinkCard card ) 
            return;

        var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
        if ( link is null ) 
            return;
        
        await page.ShowPopupAsync( 
            new QRCodePopup( $"{AppHostHelper.BaseUrl}/{link.ShortUrl}" ), 
            UiHelpers.PlainPopup());
    }
    
    private static string L(string key) => LocalizationManager.Instance.Get(key);
    private static string F(string key, params object[] args) => LocalizationManager.Instance.Format(key, args);

    private void UpdateConnectivityState(bool online)
    {
        OfflineBanner.IsVisible = !online;
        FavouritesCollection.IsEnabled = online;
    }
}
