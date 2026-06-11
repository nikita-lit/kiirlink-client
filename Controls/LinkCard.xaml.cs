using CommunityToolkit.Maui.Extensions;
using KiirLink.Services;

namespace KiirLink.Controls;

public partial class LinkCard
{
    public static readonly BindableProperty TitleProperty =
        Create(nameof(Title), "www.kiirlink.ee/forms");
    public static readonly BindableProperty UrlProperty =
        Create(nameof(Url), "https://docs.google.com/forms/d/e/...");
    public static readonly BindableProperty CategoryProperty = Create(nameof(Category), string.Empty, true);
    public static readonly BindableProperty ClicksProperty = Create(nameof(Clicks), 0, true);
    public static readonly BindableProperty DateProperty = Create(nameof(Date), "May 12, 2026");
    public static readonly BindableProperty IsFavouriteProperty = Create(nameof(IsFavourite), false, true);
    public static readonly BindableProperty LinkIdProperty = Create(nameof(LinkId), 0);
    public static readonly BindableProperty ShortUrlProperty = Create(nameof(ShortUrl), string.Empty);
    public static readonly BindableProperty ShowFavouriteIndicatorProperty =
        Create(nameof(ShowFavouriteIndicator), true, true);
    public static readonly BindableProperty ShowActionsProperty = Create(nameof(ShowActions), true);
    public static readonly BindableProperty IsPublicProperty = Create(nameof(IsPublic), true, true);
    public static readonly BindableProperty ExpirationProperty = Create(nameof(Expiration), string.Empty, true);
    public static readonly BindableProperty ExpiresAtProperty = Create<DateTime?>(nameof(ExpiresAt), null, true);

    public bool IsFavourite
    {
        get => Get<bool>(IsFavouriteProperty);
        set => Set(IsFavouriteProperty, value);
    }

    public int LinkId
    {
        get => Get<int>(LinkIdProperty);
        set => Set(LinkIdProperty, value);
    }

    public string ShortUrl
    {
        get => Get<string>(ShortUrlProperty);
        set => Set(ShortUrlProperty, value);
    }

    public bool ShowFavouriteIndicator
    {
        get => Get<bool>(ShowFavouriteIndicatorProperty);
        set => Set(ShowFavouriteIndicatorProperty, value);
    }

    public bool ShowActions
    {
        get => Get<bool>(ShowActionsProperty);
        set => Set(ShowActionsProperty, value);
    }

    public bool IsPublic
    {
        get => Get<bool>(IsPublicProperty);
        set => Set(IsPublicProperty, value);
    }

    public string Expiration
    {
        get => Get<string>(ExpirationProperty);
        set => Set(ExpirationProperty, value);
    }

    public DateTime? ExpiresAt
    {
        get => Get<DateTime?>(ExpiresAtProperty);
        set => Set(ExpiresAtProperty, value);
    }

    public string Title
    {
        get => Get<string>(TitleProperty);
        set => Set(TitleProperty, value);
    }

    public string Url
    {
        get => Get<string>(UrlProperty);
        set => Set(UrlProperty, value);
    }

    public string Category
    {
        get => Get<string>(CategoryProperty);
        set => Set(CategoryProperty, value);
    }

    public int Clicks
    {
        get => Get<int>(ClicksProperty);
        set => Set(ClicksProperty, value);
    }

    public string Date
    {
        get => Get<string>(DateProperty);
        set => Set(DateProperty, value);
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

    private T Get<T>(BindableProperty property) => (T)GetValue(property);
    private void Set<T>(BindableProperty property, T value) => SetValue(property, value);
    private static BindableProperty Create<T>(string name, T value, bool refresh = false) =>
        BindableProperty.Create(
            name,
            typeof(T),
            typeof(LinkCard),
            value,
            propertyChanged: refresh ? OnCardContentChanged : null);

    private void OnCardTapped(object? sender, TappedEventArgs e) => CardTapped?.Invoke(this, e);

    private async void OnMoreIconTapped(object? sender, TappedEventArgs e) =>
        await OnShowContextMenu(sender, e);

    protected override void OnHandlerChanging( HandlerChangingEventArgs args )
    {
        if ( args.NewHandler is null )
        {
            ThemeService.ThemeChanged -= _themeChangedHandler;
            LocalizationManager.Instance.CultureChanged -= _cultureChangedHandler;
        }

        base.OnHandlerChanging( args );
    }

    private static void OnCardContentChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((LinkCard)bindable).RefreshVisualState();

    private void RefreshVisualState()
    {
        if (CategoryBadge is null)
            return;

        var category = Category?.Trim();
        var hasCategory = !string.IsNullOrWhiteSpace(category);
        var clicks = L("Clicks");
        CategoryLabel.Text = hasCategory ? category : L("Uncategorized");
        ClicksLabel.Text = $"{Clicks} {clicks}";
        CategoryBadge.IsVisible = hasCategory;
        FavouriteIndicator.IsVisible = IsFavourite && ShowFavouriteIndicator;
        PrivateBadge.IsVisible = !IsPublic;
        var expiration = ExpiresAt.HasValue ? FormatExpiration(ExpiresAt.Value) : Expiration;
        ExpirationLabel.Text = expiration;
        ExpirationBadge.IsVisible = !string.IsNullOrWhiteSpace(expiration);
        StatusLayout.IsVisible = PrivateBadge.IsVisible || ExpirationBadge.IsVisible;
        SemanticProperties.SetDescription(
            this,
            $"{Title}. {Clicks} {clicks}. {Date}. {L(IsPublic ? "Public" : "Private")}. {expiration}. " +
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
        MoreIcon.SetTint((Color)Application.Current!.Resources["AppText"]);
    }

    private static string L(string key) => LocalizationManager.Instance.Get(key);

    private async Task OnShowContextMenu( object? sender, EventArgs e )
    {
        var page = Shell.Current?.CurrentPage;
        if ( page is null )
            return;

        var popup = new LinkActionsPopup( IsFavourite );
        var result = await page.ShowPopupAsync<LinkActionsPopupAction>(
            popup,
            UiHelpers.PlainPopup());

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
