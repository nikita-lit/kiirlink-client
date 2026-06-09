using System.Collections.ObjectModel;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Pages;

public partial class FavouritesPage : ContentPage
{
    private readonly LinkService _linkService;

    public ObservableCollection<LinkModel> Favourites { get; } = [];

    public FavouritesPage(LinkService linkService)
    {
        InitializeComponent();
        _linkService = linkService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavouritesAsync();
    }

    private async Task LoadFavouritesAsync()
    {
        try
        {
            var favourites = await _linkService.GetFavouritesAsync();

            Favourites.Clear();
            foreach (var link in favourites) Favourites.Add(link);

            FavCountLabel.Text = favourites.Count.ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load favourites: {ex.Message}", "OK");
        }
    }

    private async Task NavigateToAnalyticsAsync(LinkModel? link)
    {
        if (link is null) return;

        AnalyticsPage.SelectedLinkId = link.ResolvedId;
        AnalyticsPage.SelectedLink = link;
        await Shell.Current.GoToAsync("//Analytics");
    }

    private async void OnLinkCardTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Controls.LinkCard card) return;

        await NavigateToAnalyticsAsync(Favourites.FirstOrDefault(l => l.ResolvedId == card.LinkId));
    }

    private async void OnLinkCopyRequested(object? sender, EventArgs e)
    {
        if (sender is not Controls.LinkCard card) return;

        try
        {
            var fullUrl = $"kiirlink.ee/{card.ShortUrl}";
            await Clipboard.Default.SetTextAsync(fullUrl);
            await DisplayAlertAsync("Copied", $"Link copied: {fullUrl}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not copy link: {ex.Message}", "OK");
        }
    }

    private async void OnLinkAnalyticsRequested(object? sender, EventArgs e)
    {
        if (sender is not Controls.LinkCard card) return;

        await NavigateToAnalyticsAsync(Favourites.FirstOrDefault(l => l.ResolvedId == card.LinkId));
    }

    private async void OnLinkFavouriteToggleRequested(object? sender, EventArgs e)
    {
        if (sender is not Controls.LinkCard card) return;

        try
        {
            var success = await _linkService.RemoveFavouriteAsync(card.LinkId);
            if (success)
            {
                var link = Favourites.FirstOrDefault(l => l.ResolvedId == card.LinkId);
                if (link is not null)
                    Favourites.Remove(link);
                FavCountLabel.Text = Favourites.Count.ToString();
                await DisplayAlertAsync("Removed", "Removed from favorites.", "OK");
            }
            else
            {
                await DisplayAlertAsync("Error", "Could not update favourite status", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private async void OnLinkDeleteRequested(object? sender, EventArgs e)
    {
        if (sender is not Controls.LinkCard card) return;

        var confirm = await DisplayAlertAsync(
            "Delete link",
            $"Delete {card.Title}? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm) return;

        try
        {
            var success = await _linkService.RemoveLinkAsync(card.LinkId);
            if (success)
            {
                var link = Favourites.FirstOrDefault(l => l.ResolvedId == card.LinkId);
                if (link is not null)
                    Favourites.Remove(link);
                FavCountLabel.Text = Favourites.Count.ToString();
                await DisplayAlertAsync("Deleted", "Link has been deleted.", "OK");
            }
            else
            {
                await DisplayAlertAsync("Error", "Could not delete link", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Error: {ex.Message}", "OK");
        }
    }
}
