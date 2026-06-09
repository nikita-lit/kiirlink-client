namespace KiirLink.Controls;

public partial class LinkCard : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(LinkCard), "www.kiirlink.ee/forms");

    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create(nameof(Url), typeof(string), typeof(LinkCard), "https://docs.google.com/forms/d/e/...");

    public static readonly BindableProperty CategoryProperty =
        BindableProperty.Create(nameof(Category), typeof(string), typeof(LinkCard), "General");

    public static readonly BindableProperty ViewsProperty =
        BindableProperty.Create(nameof(Views), typeof(string), typeof(LinkCard), "120");

    public static readonly BindableProperty DateProperty =
        BindableProperty.Create(nameof(Date), typeof(string), typeof(LinkCard), "May 12, 2026");

    public static readonly BindableProperty IsFavouriteProperty =
        BindableProperty.Create(
            nameof(IsFavourite),
            typeof(bool),
            typeof(LinkCard),
            false,
            propertyChanged: OnIsFavouriteChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Url
    {
        get => (string)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    public string Category
    {
        get => (string)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public string Views
    {
        get => (string)GetValue(ViewsProperty);
        set => SetValue(ViewsProperty, value);
    }

    public string Date
    {
        get => (string)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public bool IsFavourite
    {
        get => (bool)GetValue(IsFavouriteProperty);
        set => SetValue(IsFavouriteProperty, value);
    }

    public event EventHandler<TappedEventArgs>? CardTapped;

    public LinkCard()
    {
        InitializeComponent();
        UpdateFavouriteState();
    }

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        CardTapped?.Invoke(this, e);
    }

    private static void OnIsFavouriteChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((LinkCard)bindable).UpdateFavouriteState();
    }

    private void UpdateFavouriteState()
    {
        if (CategoryBadge is null)
        {
            return;
        }

        CategoryBadge.IsVisible = !IsFavourite;
        MoreIcon.IsVisible = !IsFavourite;
        FavouriteBadge.IsVisible = IsFavourite;
    }
}
