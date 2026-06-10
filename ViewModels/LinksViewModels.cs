using System.Collections.ObjectModel;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.ViewModels;

public sealed class LinksViewModel : ViewModelBase
{
    private const int PageSize = 10;
    private readonly ILinkService _links;
    private int _currentPage = 1;
    private int _totalCount;
    private int? _selectedCategoryId;
    private LinkModel? _popularLink;

    public LinksViewModel(ILinkService links, IConnectivityService connectivity) : base(connectivity)
    {
        _links = links;
    }

    public ObservableCollection<LinkModel> Links { get; } = [];
    public ObservableCollection<CategoryModel> Categories { get; } = [];

    public LinkModel? PopularLink
    {
        get => _popularLink;
        private set
        {
            if (SetProperty(ref _popularLink, value))
                OnPropertyChanged(nameof(HasPopularLink));
        }
    }

    public bool HasPopularLink => PopularLink is not null;
    public int CurrentPage => _currentPage;
    public bool CanGoPrevious => _currentPage > 1;
    public bool CanGoNext => _currentPage * PageSize < _totalCount;

    public async Task LoadAsync()
    {
        if (!IsOnline)
            return;

        await RunBusyAsync(async () =>
        {
            var categoriesTask = _links.GetCategoriesAsync();
            var linksTask = _links.GetLinksPageAsync(_currentPage, PageSize, _selectedCategoryId);
            await Task.WhenAll(categoriesTask, linksTask);

            Categories.Clear();
            foreach (var category in await categoriesTask)
                Categories.Add(category);

            var page = await linksTask;
            Links.Clear();
            foreach (var link in page.Items)
                Links.Add(link);

            _totalCount = page.TotalCount;
            PopularLink = page.Items.MaxBy(link => link.Clicks);
            RaisePaginationProperties();
        });
    }

    public Task SelectCategoryAsync(int? categoryId)
    {
        _selectedCategoryId = categoryId;
        _currentPage = 1;
        return LoadAsync();
    }

    public Task PreviousPageAsync()
    {
        if (CanGoPrevious)
            _currentPage--;

        return LoadAsync();
    }

    public Task NextPageAsync()
    {
        if (CanGoNext)
            _currentPage++;

        return LoadAsync();
    }

    private void RaisePaginationProperties()
    {
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }
}

public sealed class FavouritesViewModel : ViewModelBase
{
    private readonly ILinkService _links;

    public FavouritesViewModel(ILinkService links, IConnectivityService connectivity) : base(connectivity)
    {
        _links = links;
    }

    public ObservableCollection<LinkModel> Favourites { get; } = [];
    public int Count => Favourites.Count;

    public async Task LoadAsync()
    {
        if (!IsOnline)
            return;

        await RunBusyAsync(async () =>
        {
            var favourites = await _links.GetFavouritesAsync();
            Favourites.Clear();
            foreach (var link in favourites)
                Favourites.Add(link);
            OnPropertyChanged(nameof(Count));
        });
    }

    public void Remove(LinkModel link)
    {
        Favourites.Remove(link);
        OnPropertyChanged(nameof(Count));
    }
}
