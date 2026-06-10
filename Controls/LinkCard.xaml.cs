using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Behaviors;
using KiirLink.Services;

namespace KiirLink.Controls;

public partial class LinkCard
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create( nameof(Title), typeof(string), typeof(LinkCard), "www.kiirlink.ee/forms" );

    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create( nameof(Url), typeof(string), typeof(LinkCard),
            "https://docs.google.com/forms/d/e/..." );

    public static readonly BindableProperty CategoryProperty =
        BindableProperty.Create( nameof(Category), typeof(string), typeof(LinkCard), string.Empty,
            propertyChanged: OnCardContentChanged );

    public static readonly BindableProperty ClicksProperty =
        BindableProperty.Create( nameof(Clicks), typeof(string), typeof(LinkCard), "120" );

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

    public static readonly BindableProperty ShowFavouriteIndicatorProperty =
        BindableProperty.Create(
            nameof(ShowFavouriteIndicator),
            typeof(bool),
            typeof(LinkCard),
            true,
            propertyChanged: OnCardContentChanged );

    public static readonly BindableProperty ShowActionsProperty =
        BindableProperty.Create( nameof(ShowActions), typeof(bool), typeof(LinkCard), true );

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

    public bool ShowFavouriteIndicator
    {
        get => (bool)GetValue( ShowFavouriteIndicatorProperty );
        set => SetValue( ShowFavouriteIndicatorProperty, value );
    }

    public bool ShowActions
    {
        get => (bool)GetValue( ShowActionsProperty );
        set => SetValue( ShowActionsProperty, value );
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

    public string Clicks
    {
        get => (string)GetValue( ClicksProperty );
        set => SetValue( ClicksProperty, value );
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
    public event EventHandler? QRCodeRequested;
    public event EventHandler? DeleteRequested;
    private readonly EventHandler _themeChangedHandler;

    public LinkCard()
    {
        InitializeComponent();
        _themeChangedHandler = (_, _) => ApplyThemeToIcons();
        ThemeService.ThemeChanged += _themeChangedHandler;
        RefreshVisualState();
        ApplyThemeToIcons();
    }

    private void OnCardTapped( object? sender, TappedEventArgs e )
    {
        CardTapped?.Invoke( this, e );
    }

    private async void OnMoreIconTapped( object? sender, TappedEventArgs e )
    {
        await OnShowContextMenu( sender, e );
    }

    protected override void OnHandlerChanging( HandlerChangingEventArgs args )
    {
        if ( args.NewHandler is null )
            ThemeService.ThemeChanged -= _themeChangedHandler;

        base.OnHandlerChanging( args );
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
        FavouriteIndicator.IsVisible = IsFavourite && ShowFavouriteIndicator;
        SemanticProperties.SetDescription(
            this,
            $"{Title}. {Clicks} views. {Date}. Tap for analytics; use link actions for more options.");
    }

    private void ApplyThemeToIcons()
    {
        SetIconTint( MoreIcon, (Color)Application.Current!.Resources["AppText"] );
    }

    private static void SetIconTint( Image image, Color tint )
    {
        var behavior = image.Behaviors.OfType<IconTintColorBehavior>().FirstOrDefault();
        if ( behavior is null )
        {
            behavior = new IconTintColorBehavior();
            image.Behaviors.Add( behavior );
        }

        behavior.TintColor = tint;
    }

    private async Task OnShowContextMenu( object? sender, EventArgs e )
    {
        var page = Shell.Current?.CurrentPage;
        if ( page is null )
            return;

        var popup = new LinkActionsPopup( IsFavourite );
        var result = await page.ShowPopupAsync<LinkActionsPopupAction>(
            popup,
            new PopupOptions
            {
                Shape = null,
                Shadow = null,
            } );

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
                    await Clipboard.Default.SetTextAsync( $"http://88.196.25.201/{ShortUrl}" );
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
            case LinkActionsPopupAction.CreateQRCode:
                QRCodeRequested?.Invoke( this, EventArgs.Empty );
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
