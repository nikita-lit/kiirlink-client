using System.Collections.ObjectModel;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Pages;

public partial class LinksPage : ContentPage
{
    private readonly LinkService _linkService;

    private Button? _activeFilter;
    private int _currentPage = 1;
    private const int PageSize = 10;
    private int? _selectedCategoryId;
    private int _selectedLinkId;

    public ObservableCollection<LinkModel> Links { get; } = [];
    public ObservableCollection<CategoryModel> Categories { get; } = [];

    public LinksPage(LinkService linkService)
    {
        InitializeComponent();
        _linkService = linkService;
        _activeFilter = AllFilter;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategoriesAsync();
        await LoadLinksAsync();
    }

    // ── Data loading ─────────────────────────────────────────────────────────

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _linkService.GetCategoriesAsync();

            // Keep "All" button, remove old category chips
            var layout = AllFilter.Parent as HorizontalStackLayout;
            if (layout is not null)
            {
                // Remove all chips after AllFilter
                while (layout.Children.Count > 1)
                    layout.Children.RemoveAt(1);

                foreach (var cat in categories)
                {
                    var chip = new Button
                    {
                        Text = cat.Name,
                        CommandParameter = cat.Id.ToString(),
                        Style = (Style)Application.Current!.Resources["ChipButton"]
                    };
                    chip.Clicked += OnFilterClicked;
                    layout.Children.Add(chip);
                }
            }

            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);
        }
        catch
        {
            // ignore, categories are optional
        }
    }

    private async Task LoadLinksAsync()
    {
        try
        {
            var links = await _linkService.GetLinksAsync(_currentPage, PageSize, _selectedCategoryId);

            Links.Clear();
            foreach (var link in links) Links.Add(link);

            UpdatePopularCard(links);
            UpdatePagination(links.Count);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load links: {ex.Message}", "OK");
        }
    }

    private void UpdatePopularCard(List<LinkModel> links)
    {
        if (links.Count == 0)
        {
            PopularCard.IsVisible = false;
            PopularCardEmpty.IsVisible = true;
            return;
        }

        var top = links.MaxBy(l => l.Views)!;
        _selectedLinkId = top.Id;

        PopularViews.Text = top.DisplayViews;
        PopularTitle.Text = top.DisplayTitle;
        PopularUrl.Text = top.OriginalUrl;
        PopularCategory.Text = top.CategoryName ?? string.Empty;
        PopularCategoryBadge.IsVisible = top.CategoryName is not null;

        PopularCard.IsVisible = true;
        PopularCardEmpty.IsVisible = false;
    }

    private void UpdatePagination(int loadedCount)
    {
        PaginationRow.Children.Clear();

        // Show prev page if not on first
        if (_currentPage > 1)
        {
            var prev = MakePageButton("‹", _currentPage - 1, false);
            PaginationRow.Children.Add(prev);
        }

        var current = MakePageButton(_currentPage.ToString(), _currentPage, true);
        PaginationRow.Children.Add(current);

        // Show next only if a full page was loaded (more may exist)
        if (loadedCount >= PageSize)
        {
            var next = MakePageButton("›", _currentPage + 1, false);
            PaginationRow.Children.Add(next);
        }
    }

    private Button MakePageButton(string text, int page, bool active)
    {
        var btn = new Button
        {
            Text = text,
            Style = (Style)Application.Current!.Resources["ChipButton"]
        };

        if (active)
        {
            btn.BackgroundColor = (Color)Application.Current.Resources["SoftOrange"];
            btn.TextColor = (Color)Application.Current.Resources["BrandOrange"];
            btn.BorderColor = (Color)Application.Current.Resources["BrandOrange"];
        }

        btn.Clicked += async (_, _) =>
        {
            _currentPage = page;
            await LoadLinksAsync();
        };

        return btn;
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private async void OnNewLinkClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Home");
    }

    private async Task NavigateToAnalyticsAsync(LinkModel? link = null, int? fallbackLinkId = null)
    {
        if (link is not null)
        {
            _selectedLinkId = link.Id;
            AnalyticsPage.SelectedLinkId = link.Id;
            AnalyticsPage.SelectedLink = link;
        }
        else
        {
            var id = fallbackLinkId ?? _selectedLinkId;
            _selectedLinkId = id;
            AnalyticsPage.SelectedLinkId = id;
            AnalyticsPage.SelectedLink = Links.FirstOrDefault(l => l.Id == id);
        }

        await Shell.Current.GoToAsync("//Analytics");
    }

    private async void OnAnalyticsTapped(object? sender, TappedEventArgs e)
    {
        await NavigateToAnalyticsAsync(Links.FirstOrDefault(l => l.Id == _selectedLinkId));
    }

    private async void OnLinkCardTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Controls.LinkCard card)
            await NavigateToAnalyticsAsync(Links.FirstOrDefault(l => l.Id == card.LinkId), card.LinkId);
        else
            await NavigateToAnalyticsAsync();
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

        await NavigateToAnalyticsAsync(Links.FirstOrDefault(l => l.Id == card.LinkId), card.LinkId);
    }

    private async void OnLinkFavouriteToggleRequested(object? sender, EventArgs e)
    {
        if (sender is not Controls.LinkCard card) return;

        try
        {
            var link = Links.FirstOrDefault(l => l.Id == card.LinkId);
            if (link is null) return;

            bool success;
            if (link.IsFavourite)
            {
                success = await _linkService.RemoveFavouriteAsync(card.LinkId);
                if (success) link.IsFavourite = false;
            }
            else
            {
                success = await _linkService.AddFavouriteAsync(card.LinkId);
                if (success) link.IsFavourite = true;
            }

            if (!success)
                await DisplayAlertAsync("Error", "Could not update favourite status", "OK");
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
                Links.RemoveAt(Links.IndexOf(Links.FirstOrDefault(l => l.Id == card.LinkId)!));
                await DisplayAlertAsync("Deleted", "Link has been deleted.", "OK");
                await LoadLinksAsync(); // Reload to update pagination and popular card
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

    private async void OnFilterClicked(object? sender, EventArgs e)
    {
        if (sender is not Button selected) return;

        // Visual state
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

        // Update filter
        var param = selected.CommandParameter?.ToString();
        _selectedCategoryId = param is "0" or null ? null : int.Parse(param);
        _currentPage = 1;

        await LoadLinksAsync();
    }
}
