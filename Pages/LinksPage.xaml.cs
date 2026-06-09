namespace KiirLink.Pages;

public partial class LinksPage : ContentPage
{
    private Button? _activeFilter;

    public LinksPage()
    {
        InitializeComponent();
        _activeFilter = AllFilter;
    }

    private async void OnNewLinkClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Home");
    }

    private async void OnAnalyticsTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//Analytics");
    }

    private async void OnLinkCardTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//Analytics");
    }

    private void OnFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button selected)
        {
            return;
        }

        if (_activeFilter is not null)
        {
            _activeFilter.BackgroundColor = Colors.White;
            _activeFilter.TextColor = Color.FromArgb("#343434");
            _activeFilter.BorderColor = Color.FromArgb("#E6E6E6");
        }

        selected.BackgroundColor = Color.FromArgb("#FFF0EC");
        selected.TextColor = Color.FromArgb("#FF5A36");
        selected.BorderColor = Color.FromArgb("#FF5A36");
        _activeFilter = selected;
    }
}
