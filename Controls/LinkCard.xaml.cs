using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace KiirLink.Controls;

public partial class LinkCard : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create( nameof(Title), typeof(string), typeof(LinkCard), "www.kiirlink.ee/forms" );

    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create( nameof(Url), typeof(string), typeof(LinkCard),
            "https://docs.google.com/forms/d/e/..." );

    public static readonly BindableProperty CategoryProperty =
        BindableProperty.Create( nameof(Category), typeof(string), typeof(LinkCard), string.Empty,
            propertyChanged: OnCardContentChanged );

    public static readonly BindableProperty ViewsProperty =
        BindableProperty.Create( nameof(Views), typeof(string), typeof(LinkCard), "120" );

    public static readonly BindableProperty DateProperty =
        BindableProperty.Create( nameof(Date), typeof(string), typeof(LinkCard), "May 12, 2026" );

    public static readonly BindableProperty IsFavouriteProperty =
        BindableProperty.Create(
            nameof(IsFavourite),
            typeof(bool),
            typeof(LinkCard),
            false,
            propertyChanged: OnIsFavouriteChanged );

    public static readonly BindableProperty LinkIdProperty =
        BindableProperty.Create( nameof(LinkId), typeof(int), typeof(LinkCard), 0 );

    public static readonly BindableProperty ShortUrlProperty =
        BindableProperty.Create( nameof(ShortUrl), typeof(string), typeof(LinkCard), "" );

    public bool IsFavourite
    {
        get => (bool)GetValue( IsFavouriteProperty );
        set => SetValue( IsFavouriteProperty, value );
    }

    public int LinkId
    {
        get => (int)GetValue( LinkIdProperty );
        set => SetValue( LinkIdProperty, value );
    }

    public string ShortUrl
    {
        get => (string)GetValue( ShortUrlProperty );
        set => SetValue( ShortUrlProperty, value );
    }

    public string Title
    {
        get => (string)GetValue( TitleProperty );
        set => SetValue( TitleProperty, value );
    }

    public string Url
    {
        get => (string)GetValue( UrlProperty );
        set => SetValue( UrlProperty, value );
    }

    public string Category
    {
        get => (string)GetValue( CategoryProperty );
        set => SetValue( CategoryProperty, value );
    }

    public string Views
    {
        get => (string)GetValue( ViewsProperty );
        set => SetValue( ViewsProperty, value );
    }

    public string Date
    {
        get => (string)GetValue( DateProperty );
        set => SetValue( DateProperty, value );
    }

    public event EventHandler<TappedEventArgs>? CardTapped;
    public event EventHandler? AnalyticsRequested;
    public event EventHandler? FavouriteToggleRequested;
    public event EventHandler? CategoryRequested;
    public event EventHandler? CategoryDeleteRequested;
    public event EventHandler? DeleteRequested;

    public LinkCard()
    {
        InitializeComponent();
        RefreshVisualState();
    }

    private void OnCardTapped( object? sender, TappedEventArgs e )
    {
        CardTapped?.Invoke( this, e );
    }

    private async void OnMoreIconTapped( object? sender, TappedEventArgs e )
    {
        await OnShowContextMenu( sender, e );
    }

    private void OnFavouriteTapped( object? sender, TappedEventArgs e )
    {
        FavouriteToggleRequested?.Invoke( this, EventArgs.Empty );
    }

    private static void OnIsFavouriteChanged( BindableObject bindable, object oldValue, object newValue )
    {
        ((LinkCard)bindable).RefreshVisualState();
    }

    private static void OnCardContentChanged( BindableObject bindable, object oldValue, object newValue )
    {
        ((LinkCard)bindable).RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        if ( CategoryBadge is null )
        {
            return;
        }

        var category = Category?.Trim();
        var hasCategory = !string.IsNullOrWhiteSpace( category );
        CategoryLabel.Text = hasCategory ? category : "Uncategorized";
        CategoryBadge.IsVisible = hasCategory;
        FavouriteBadge.IsVisible = IsFavourite;
    }

    private async Task OnShowContextMenu( object? sender, EventArgs e )
    {
        var page = Shell.Current?.CurrentPage;
        if ( page is null )
            return;

        var popup = new LinkActionsPopup( !string.IsNullOrWhiteSpace( Category ), IsFavourite );
        var result = await page.ShowPopupAsync<LinkActionsPopupAction>( popup );

        if ( result.WasDismissedByTappingOutsideOfPopup )
            return;

        var action = result.Result;

        switch ( action )
        {
            case LinkActionsPopupAction.OpenOriginal:
                if ( !string.IsNullOrWhiteSpace( Url ) )
                    await Launcher.Default.OpenAsync( Url );
                break;
            case LinkActionsPopupAction.CopyShort:
                if ( !string.IsNullOrWhiteSpace( ShortUrl ) )
                    await Clipboard.Default.SetTextAsync( $"https://kiirlink.ee/{ShortUrl}" );
                break;
            case LinkActionsPopupAction.CopyOriginal:
                if ( !string.IsNullOrWhiteSpace( Url ) )
                    await Clipboard.Default.SetTextAsync( Url );
                break;
            case LinkActionsPopupAction.ViewAnalytics:
                AnalyticsRequested?.Invoke( this, EventArgs.Empty );
                break;
            case LinkActionsPopupAction.AssignCategory:
                CategoryRequested?.Invoke( this, EventArgs.Empty );
                break;
            case LinkActionsPopupAction.DeleteCategory:
                CategoryDeleteRequested?.Invoke( this, EventArgs.Empty );
                break;
            case LinkActionsPopupAction.ToggleFavourite:
                FavouriteToggleRequested?.Invoke( this, EventArgs.Empty );
                break;
            case LinkActionsPopupAction.Delete:
                DeleteRequested?.Invoke( this, EventArgs.Empty );
                break;
        }
    }
}