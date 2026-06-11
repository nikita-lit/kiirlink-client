using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Extensions;
using KiirLink.Controls;
using KiirLink.Extensions;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Pages;

public partial class FavouritesPage
{
    private readonly ApiClient _api;
    private readonly IConnectivityService _connectivity;
    private readonly EventHandler<bool> _connectivityChangedHandler;
    private bool _isLinkActionsPopupOpen;

    public ObservableCollection<LinkModel> Favourites { get; } = [];

    public FavouritesPage(ApiClient api, IConnectivityService connectivity)
    {
        InitializeComponent();
        _api = api;
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
        if (_connectivity.IsOnline && !_isLinkActionsPopupOpen)
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
            var favourites = await _api.GetFavouritesAsync();

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

    private async void OnLinkAnalyticsRequested( object? sender, EventArgs e )
    {
        if (sender is LinkCard card)
            await NavigateToAnalyticsAsync(Favourites.FirstOrDefault(link => link.ResolvedId == card.LinkId));
    }

    private async void OnLinkCategoryRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) 
            return;

        var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
        if ( link is null ) return;

        var category = await UiHelpers.AssignCategoryAsync(
            this,
            _api,
            link.ResolvedId,
            await _api.GetCategoriesAsync());
        if ( category is null )
            return;

        link.CategoryId = category.Id;
        link.CategoryName = category.Name;
        await LoadFavouritesAsync();
    }
    
    private async void OnLinkFavouriteToggleRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) return;

        if (!await UiHelpers.SetFavouriteAsync(this, _api, card.LinkId, false))
            return;

        RemoveFavourite(card.LinkId);
        await DisplayAlertAsync(L("Removed"), L("RemovedFromFavourites"), "OK");
    }

    private async void OnLinkDeleteRequested( object? sender, EventArgs e )
    {
        if (sender is not LinkCard card ||
            !await UiHelpers.DeleteLinkAsync(this, _api, card.LinkId, card.Title))
            return;

        RemoveFavourite(card.LinkId);
        await DisplayAlertAsync(L("Deleted"), L("LinkDeleted"), "OK");
    }

    private async void OnCreateQRCodeClicked( object? sender, EventArgs e )
    {
        if (sender is not LinkCard card || Shell.Current?.CurrentPage is not { } page)
            return;

        var link = Favourites.FirstOrDefault( l => l.ResolvedId == card.LinkId );
        if ( link is null ) 
            return;
        
        await UiHelpers.ShowQrCodeAsync(page, link);
    }

    private void RemoveFavourite(int linkId)
    {
        if (Favourites.FirstOrDefault(link => link.ResolvedId == linkId) is { } link)
            Favourites.Remove(link);
        FavCountLabel.Text = Favourites.Count.ToString();
    }

    private void OnLinkActionsPopupVisibilityChanged(object? sender, bool isOpen) =>
        _isLinkActionsPopupOpen = isOpen;
    
    private static string L(string key) => LocalizationManager.Instance.Get(key);
    private static string F(string key, params object[] args) => LocalizationManager.Instance.Format(key, args);

    private void UpdateConnectivityState(bool online)
    {
        OfflineBanner.IsVisible = !online;
        FavouritesCollection.IsEnabled = online;
    }
}
