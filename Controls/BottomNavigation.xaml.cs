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
        UpdateColors();
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
    }

    private static Task NavigateAsync( string route )
    {
        return Shell.Current.GoToAsync( $"//{route}" );
    }

    private async void OnHomeTapped( object? sender, TappedEventArgs e ) =>
        await NavigateAsync( "Home" );

    private async void OnHomeClicked( object? sender, EventArgs e ) =>
        await NavigateAsync( "Home" );

    private async void OnLinksTapped( object? sender, TappedEventArgs e ) =>
        await NavigateAsync( "Links" );

    private async void OnLinksClicked( object? sender, EventArgs e ) =>
        await NavigateAsync( "Links" );

    private async void OnFavouritesTapped( object? sender, TappedEventArgs e ) =>
        await NavigateAsync( "Favourites" );

    private async void OnFavouritesClicked( object? sender, EventArgs e ) =>
        await NavigateAsync( "Favourites" );

    private async void OnProfileTapped( object? sender, TappedEventArgs e ) =>
        await NavigateAsync( "Profile" );

    private async void OnProfileClicked( object? sender, EventArgs e ) =>
        await NavigateAsync( "Profile" );
}