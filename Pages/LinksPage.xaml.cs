using System.Collections.ObjectModel;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using KiirLink.Controls;
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

            Categories.Clear();
            foreach ( var c in categories ) Categories.Add( c );
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
            var links = page.Items;

            Links.Clear();
            foreach ( var link in links ) Links.Add( link );

            UpdatePopularCard( links );
            _totalLinkCount = page.TotalCount;
            UpdatePagination( _totalLinkCount );
        }
        catch ( Exception ex )
        {
            await DisplayAlertAsync( "Error", $"Could not load links: {ex.Message}", "OK" );
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
            new PopupOptions
            {
                Shape = null,
                Shadow = null,
            } );
        
        await LoadCategoriesAsync();
        await LoadLinksAsync();
    }

    private async void OnCreateQRCodeClicked( object? sender, EventArgs e )
    {
        var page = Shell.Current?.CurrentPage;
        if ( page is null )
            return;
        
        if ( sender is not LinkCard card ) 
            return;

        var link = Links.FirstOrDefault( l => l.Id == card.LinkId );
        if ( link is null ) 
            return;
        
        await page.ShowPopupAsync( 
            new QRCodePopup( $"{AppHostHelper.BaseUrl}/{link.ShortUrl}" ), 
            new PopupOptions
            {
                Shape = null,
                Shadow = null,
            } );
        
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

        var category = await PromptAssignExistingCategoryAsync( link.Id );
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

        try
        {
            var link = Links.FirstOrDefault( l => l.Id == card.LinkId );
            if ( link is null ) return;

            bool success;
            if ( link.IsFavourite )
            {
                success = await _linkService.RemoveFavouriteAsync( card.LinkId );
                if ( success ) link.IsFavourite = false;
            }
            else
            {
                success = await _linkService.AddFavouriteAsync( card.LinkId );
                if ( success ) link.IsFavourite = true;
            }

            if ( !success )
                await DisplayAlertAsync( "Error", "Could not update favourite status", "OK" );
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
                Links.RemoveAt( Links.IndexOf( Links.FirstOrDefault( l => l.Id == card.LinkId )! ) );
                await DisplayAlertAsync( "Deleted", "Link has been deleted.", "OK" );
                await LoadLinksAsync();
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

    private async Task<CategoryModel?> PromptAssignExistingCategoryAsync( int linkId )
    {
        var categories = Categories.ToList();
        if ( categories.Count == 0 )
        {
            await DisplayAlertAsync( "No categories", "Create a category first from Categories.", "OK" );
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
