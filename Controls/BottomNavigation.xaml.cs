using KiirLink.Services;

namespace KiirLink.Controls;

public partial class BottomNavigation : ContentView
{
    public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
        nameof(ActiveTab),
        typeof(string),
        typeof(BottomNavigation),
        "Home",
        propertyChanged: OnActiveTabChanged );

    public string ActiveTab
    {
        get => (string)GetValue( ActiveTabProperty );
        set => SetValue( ActiveTabProperty, value );
    }

    public BottomNavigation()
    {
        InitializeComponent();
        ThemeService.ThemeChanged += OnThemeChanged;
        UpdateColors();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent is null)
            ThemeService.ThemeChanged -= OnThemeChanged;
    }

    private static void OnActiveTabChanged( BindableObject bindable, object oldValue, object newValue )
    {
        ((BottomNavigation)bindable).UpdateColors();
    }

    private void UpdateColors()
    {
        if ( HomeIcon is null )
        {
            return;
        }

        SetTabIcon( HomeIcon, "hometab", ActiveTab == "Home" );
        SetTabIcon( LinksIcon, "linkstab", ActiveTab == "Links" );
        SetTabIcon( FavouritesIcon, "startab", ActiveTab == "Favourites" );
        SetTabIcon( ProfileIcon, "usertab", ActiveTab == "Profile" );
    }

    private static void SetTabIcon( Image icon, string name, bool isActive )
    {
        icon.Source = isActive ? $"{name}_active.png" : $"{name}.png";
        icon.Behaviors.Clear();

        if (!isActive)
            icon.SetTint((Color)Application.Current!.Resources["AppMutedText"]);
    }

    private void OnThemeChanged( object? sender, EventArgs e )
    {
        UpdateColors();
    }

    private static Task NavigateAsync(object? route) =>
        Shell.Current.GoToAsync($"//{route}");

    private async void OnTabTapped(object? sender, TappedEventArgs e) =>
        await NavigateAsync(e.Parameter);

    private async void OnTabClicked(object? sender, EventArgs e) =>
        await NavigateAsync(((Button)sender!).CommandParameter);
}
