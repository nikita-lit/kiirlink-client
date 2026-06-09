namespace KiirLink.Pages;

public partial class FavouritesPage : ContentPage
{
    public FavouritesPage()
    {
        InitializeComponent();
    }

    private async void OnLinkCardTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//Analytics");
    }
}
