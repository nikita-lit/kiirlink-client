using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Extensions;
using KiirLink.Controls;
using KiirLink.Extensions;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Pages;

public partial class LinksPage
{
    private readonly LinkService _linkService;
    private readonly IConnectivityService _connectivity;
    private readonly EventHandler _themeChangedHandler;
    private readonly EventHandler<bool> _connectivityChangedHandler;

    private Button? _activeFilter;
    private int _currentPage = 1;
    private const int PageSize = 10;
    private int? _selectedCategoryId;
    private int _selectedLinkId;
    private int _totalLinkCount;

    public ObservableCollection<LinkModel> Links { get; } = [];
    public ObservableCollection<CategoryModel> Categories { get; } = [];

    public LinksPage(LinkService linkService, IConnectivityService connectivity)
    {
        InitializeComponent();
        _linkService = linkService;
        _connectivity = connectivity;
        _themeChangedHandler = (_, _) => RefreshFilterStyles();
        _connectivityChangedHandler = (_, online) =>
            Dispatcher.Dispatch(() => UpdateConnectivityState(online));
        _activeFilter = AllFilter;
        BindingContext = this;
        RefreshFilterStyles();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.ThemeChanged += _themeChangedHandler;
        _connectivity.ConnectivityChanged += _connectivityChangedHandler;
        UpdateConnectivityState(_connectivity.IsOnline);
        RefreshFilterStyles();
        if (_connectivity.IsOnline)
        {
            await LoadCategoriesAsync();
            await LoadLinksAsync();
        }
    }

    protected override void OnDisappearing()
    {
        ThemeService.ThemeChanged -= _themeChangedHandler;
        _connectivity.ConnectivityChanged -= _connectivityChangedHandler;
        base.OnDisappearing();
    }

    // ── Data loading ─────────────────────────────────────────────────────────

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _linkService.GetCategoriesAsync();

            // Keep "All" button, remove old category chips
            var layout = AllFilter.Parent as HorizontalStackLayout;
            if ( layout is not null )
            {
                // Remove all chips after AllFilter
                while ( layout.Children.Count > 1 )
                    layout.Children.RemoveAt( 1 );

                foreach ( var cat in categories )
                {
                    var chip = new Button
                    {
                        Text = cat.Name,
                        CommandParameter = cat.Id.ToString(),
                        Style = (Style)Application.Current!.Resources["ChipButton"],
                        MinimumWidthRequest = 50,
                    };
                    chip.Clicked += OnFilterClicked;
                    layout.Children.Add( chip );
                }
            }

            Categories.ReplaceWith(categories);
            RefreshFilterStyles();
        }
        catch
        {
            // ignore, categories are optional
        }
    }

    private async Task LoadLinksAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoadingSkeleton.IsVisible = true;
        try
        {
            var page = await _linkService.GetLinksPageAsync( _currentPage, PageSize, _selectedCategoryId );
            var lastPage = page.GetLastPage( PageSize );

            if ( _currentPage > lastPage )
            {
                _currentPage = lastPage;
                page = await _linkService.GetLinksPageAsync( _currentPage, PageSize, _selectedCategoryId );
            }

            var links = page.Items;

            Links.ReplaceWith(links);

            UpdatePopularCard( links );
            _totalLinkCount = page.TotalCount;
            UpdatePagination( _totalLinkCount );
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( L("Error"), F("CouldNotLoadLinks", ex.Message), "OK" );
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            LoadingSkeleton.IsVisible = false;
        }
    }

    private void UpdatePopularCard( List<LinkModel> links )
    {
        if ( links.Count == 0 )
        {
            PopularCard.IsVisible = false;
            return;
        }

        var top = links.MaxBy( l => l.Clicks )!;
        _selectedLinkId = top.Id;

        PopularViews.Text = top.DisplayClicks;
        PopularTitle.Text = top.DisplayTitle;
        PopularUrl.Text = top.OriginalUrl;
        PopularCategory.Text = top.CategoryName ?? string.Empty;
        PopularCategoryBadge.IsVisible = top.CategoryName is not null;

        PopularCard.IsVisible = true;
    }

    private void UpdatePagination( int totalCount )
    {
        PaginationRow.Children.Clear();

        // Show prev page if not on first
        if ( _currentPage > 1 )
        {
            var prev = MakePageButton( "‹", _currentPage - 1, false );
            PaginationRow.Children.Add( prev );
        }

        var current = MakePageButton( _currentPage.ToString(), _currentPage, true );
        PaginationRow.Children.Add( current );

        // Show next only if more items exist after this page.
        if ( _currentPage * PageSize < totalCount )
        {
            var next = MakePageButton( "›", _currentPage + 1, false );
            PaginationRow.Children.Add( next );
        }
    }

    private Button MakePageButton( string text, int page, bool active )
    {
        var btn = new Button
        {
            Text = text,
            Style = (Style)Application.Current!.Resources["ChipButton"],
            MinimumWidthRequest = 40,
            FontSize = 15,
        };

        if ( active )
        {
            btn.BackgroundColor = (Color)Application.Current.Resources["AppAccentSurface"];
            btn.TextColor = (Color)Application.Current.Resources["AppAccentText"];
            btn.BorderColor = (Color)Application.Current.Resources["AppAccentStroke"];
            btn.IsEnabled = false;
        }

        btn.Clicked += async ( _, _ ) =>
        {
            _currentPage = page;
            await LoadLinksAsync();
        };

        return btn;
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private async void OnNewLinkClicked( object? sender, EventArgs e )
    {
        await Shell.Current.GoToAsync( "//Home" );
    }

    private async void OnManageCategoriesClicked( object? sender, EventArgs e )
    {
        var page = Shell.Current?.CurrentPage;
        if ( page is null )
            return;
        
        await page.ShowPopupAsync( 
            new CategoryManagementPopup( _linkService, _connectivity ), 
            UiHelpers.PlainPopup());
        
        await LoadCategoriesAsync();
        await LoadLinksAsync();
    }

    private async void OnCreateQRCodeClicked( object? sender, EventArgs e )
    {
        if (sender is not LinkCard card || Shell.Current?.CurrentPage is not { } page)
            return;

        var link = Links.FirstOrDefault( l => l.Id == card.LinkId );
        if ( link is null ) 
            return;
        
        await UiHelpers.ShowQrCodeAsync(page, link);
        
        await LoadCategoriesAsync();
        await LoadLinksAsync();
    }
    
    private async Task NavigateToAnalyticsAsync( LinkModel? link = null, int? fallbackLinkId = null )
    {
        if ( link is not null )
        {
            _selectedLinkId = link.Id;
            AnalyticsPage.SelectedLinkId = link.Id;
            AnalyticsPage.SelectedLink = link;
        }
        else
        {
            var id = fallbackLinkId ?? _selectedLinkId;
            _selectedLinkId = id;
            AnalyticsPage.SelectedLinkId = id;
            AnalyticsPage.SelectedLink = Links.FirstOrDefault( l => l.Id == id );
        }

        await Shell.Current.GoToAsync( "//Analytics" );
    }

    private async void OnAnalyticsTapped( object? sender, TappedEventArgs e )
    {
        await NavigateToAnalyticsAsync( Links.FirstOrDefault( l => l.Id == _selectedLinkId ) );
    }

    private async void OnLinkCardTapped( object? sender, TappedEventArgs e )
    {
        if ( sender is LinkCard card )
            await NavigateToAnalyticsAsync( Links.FirstOrDefault( l => l.Id == card.LinkId ), card.LinkId );
        else
            await NavigateToAnalyticsAsync();
    }

    private async void OnLinkAnalyticsRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) 
            return;

        await NavigateToAnalyticsAsync( Links.FirstOrDefault( l => l.Id == card.LinkId ), card.LinkId );
    }

    private async void OnLinkCategoryRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) 
            return;

        var link = Links.FirstOrDefault( l => l.Id == card.LinkId );
        if ( link is null ) 
            return;

        var category = await UiHelpers.AssignCategoryAsync(this, _linkService, link.Id, Categories);
        if ( category is null )
            return;

        link.CategoryId = category.Id;
        link.CategoryName = category.Name;
        await LoadCategoriesAsync();
        await LoadLinksAsync();
    }

    private async void OnLinkFavouriteToggleRequested( object? sender, EventArgs e )
    {
        if ( sender is not LinkCard card ) 
            return;

        var link = Links.FirstOrDefault(item => item.Id == card.LinkId);
        if (link is not null &&
            await UiHelpers.SetFavouriteAsync(this, _linkService, card.LinkId, !link.IsFavourite))
            link.IsFavourite = !link.IsFavourite;
    }

    private async void OnLinkDeleteRequested( object? sender, EventArgs e )
    {
        if (sender is not LinkCard card ||
            !await UiHelpers.DeleteLinkAsync(this, _linkService, card.LinkId, card.Title))
            return;

        Links.Remove(Links.First(link => link.Id == card.LinkId));
        await DisplayAlertAsync(L("Deleted"), L("LinkDeleted"), "OK");
        await LoadLinksAsync();
    }

    private async void OnFilterClicked( object? sender, EventArgs e )
    {
        if ( sender is not Button selected ) 
            return;
        
        if ( _activeFilter is not null )
            RefreshFilterStyles();
        
        _activeFilter = selected;
        RefreshFilterStyles();
        
        var param = selected.CommandParameter?.ToString();
        _selectedCategoryId = param is "0" or null ? null : int.Parse( param );
        _currentPage = 1;

        await LoadLinksAsync();
    }

    private static string L(string key) => LocalizationManager.Instance.Get(key);
    private static string F(string key, params object[] args) => LocalizationManager.Instance.Format(key, args);

    private void RefreshFilterStyles()
    {
        if ( AllFilter.Parent is not HorizontalStackLayout layout )
            return;

        foreach ( var child in layout.Children.OfType<Button>() )
            ApplyFilterStyle( child, child == _activeFilter );
    }

    private static void ApplyFilterStyle( Button button, bool active )
    {
        var resources = Application.Current!.Resources;
        button.BackgroundColor = (Color)resources[active ? "AppAccentSurface" : "AppSurface"];
        button.TextColor = (Color)resources[active ? "AppAccentText" : "AppText"];
        button.BorderColor = (Color)resources[active ? "AppAccentStroke" : "AppStroke"];
    }

    private void UpdateConnectivityState(bool online)
    {
        OfflineBanner.IsVisible = !online;
        LinksCollection.IsEnabled = online;
    }
}
