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
        BindableProperty.Create(
            nameof(Clicks),
            typeof(int),
            typeof(LinkCard),
            0,
            propertyChanged: OnCardContentChanged );

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

    public static readonly BindableProperty IsPublicProperty =
        BindableProperty.Create(
            nameof(IsPublic),
            typeof(bool),
            typeof(LinkCard),
            true,
            propertyChanged: OnCardContentChanged );

    public static readonly BindableProperty ExpirationProperty =
        BindableProperty.Create(
            nameof(Expiration),
            typeof(string),
            typeof(LinkCard),
            string.Empty,
            propertyChanged: OnCardContentChanged );

    public static readonly BindableProperty ExpiresAtProperty =
        BindableProperty.Create(
            nameof(ExpiresAt),
            typeof(DateTime?),
            typeof(LinkCard),
            null,
            propertyChanged: OnCardContentChanged );

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

    public bool IsPublic
    {
        get => (bool)GetValue( IsPublicProperty );
        set => SetValue( IsPublicProperty, value );
    }

    public string Expiration
    {
        get => (string)GetValue( ExpirationProperty );
        set => SetValue( ExpirationProperty, value );
    }

    public DateTime? ExpiresAt
    {
        get => (DateTime?)GetValue( ExpiresAtProperty );
        set => SetValue( ExpiresAtProperty, value );
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

    public int Clicks
    {
        get => (int)GetValue( ClicksProperty );
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
    private readonly EventHandler _cultureChangedHandler;

    public LinkCard()
    {
        InitializeComponent();
        _themeChangedHandler = (_, _) => ApplyThemeToIcons();
        _cultureChangedHandler = (_, _) => RefreshVisualState();
        ThemeService.ThemeChanged += _themeChangedHandler;
        LocalizationManager.Instance.CultureChanged += _cultureChangedHandler;
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
        {
            ThemeService.ThemeChanged -= _themeChangedHandler;
            LocalizationManager.Instance.CultureChanged -= _cultureChangedHandler;
        }

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
        CategoryLabel.Text = hasCategory ? category : LocalizationManager.Instance.Get("Uncategorized");
        ClicksLabel.Text = $"{Clicks} {LocalizationManager.Instance.Get("Clicks")}";
        CategoryBadge.IsVisible = hasCategory;
        FavouriteIndicator.IsVisible = IsFavourite && ShowFavouriteIndicator;
        PrivateBadge.IsVisible = !IsPublic;
        var expiration = ExpiresAt.HasValue ? FormatExpiration(ExpiresAt.Value) : Expiration;
        ExpirationLabel.Text = expiration;
        ExpirationBadge.IsVisible = !string.IsNullOrWhiteSpace(expiration);
        StatusLayout.IsVisible = PrivateBadge.IsVisible || ExpirationBadge.IsVisible;
        SemanticProperties.SetDescription(
            this,
            $"{Title}. {Clicks} {LocalizationManager.Instance.Get("Clicks")}. {Date}. " +
            $"{LocalizationManager.Instance.Get(IsPublic ? "Public" : "Private")}. {expiration}. " +
            "Tap for analytics; use link actions for more options.");
    }

    private static string FormatExpiration(DateTime expiresAt)
    {
        var localization = LocalizationManager.Instance;
        var remaining = expiresAt.ToUniversalTime() - DateTime.UtcNow;

        if (remaining <= TimeSpan.Zero)
            return localization.Get("Expired");

        if (remaining.TotalHours < 24)
            return localization.Format("ExpiresInHours",
                Math.Max(1, (int)Math.Ceiling(remaining.TotalHours)));

        if (remaining.TotalDays < 30)
            return localization.Format("ExpiresInDays",
                Math.Max(1, (int)Math.Ceiling(remaining.TotalDays)));

        return localization.Format(
            "ExpiresOn",
            expiresAt.ToLocalTime().ToString("d MMM yyyy", localization.CurrentCulture));
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
                    await Clipboard.Default.SetTextAsync( $"{AppHostHelper.BaseUrl}/{ShortUrl}" );
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
