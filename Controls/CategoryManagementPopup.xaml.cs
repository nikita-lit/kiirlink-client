using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Extensions;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Controls;

public partial class CategoryManagementPopup
{
    private readonly LinkService _linkService;
    private readonly IConnectivityService _connectivity;
    private readonly ObservableCollection<CategoryModel> _categories = [];
    private bool _loaded;

    public ObservableCollection<CategoryModel> Categories => _categories;

    public CategoryManagementPopup(LinkService linkService, IConnectivityService connectivity)
    {
        InitializeComponent();
        _linkService = linkService;
        _connectivity = connectivity;
        BindingContext = this;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        if (_loaded)
            return;

        _loaded = true;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (!_connectivity.IsOnline)
        {
            await Shell.Current!.CurrentPage!.DisplayAlertAsync(
                L("Offline"),
                L("ConnectToManageCategories"),
                "OK");
            await CloseAsync();
            return;
        }

        try
        {
            var categories = await _linkService.GetCategoriesAsync();
            _categories.Clear();
            foreach (var category in categories)
                _categories.Add(category);
        }
        catch (Exception ex)
        {
            await Shell.Current!.CurrentPage!.DisplayAlertAsync(
                L("Error"),
                F("CouldNotLoadCategories", ex.Message),
                "OK");
            await CloseAsync();
        }
    }

    private async void OnCreateClicked(object? sender, EventArgs e)
    {
        var name = NewCategoryEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            var created = await _linkService.CreateCategoryAsync(name);
            if (created is null)
            {
                await Shell.Current!.CurrentPage!.DisplayAlertAsync(
                    L("Error"),
                    L("CouldNotCreateCategory"),
                    "OK");
                return;
            }

            NewCategoryEntry.Text = string.Empty;
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current!.CurrentPage!.DisplayAlertAsync(
                L("Error"),
                F("CouldNotCreateCategoryDetails", ex.Message),
                "OK");
        }
    }

    private async void OnDeleteCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not CategoryModel category)
            return;

        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return;

        var confirm = await page.DisplayAlertAsync(
            L("DeleteCategory"),
            F("DeleteCategoryConfirmation", category.Name),
            L("Delete"),
            L("Cancel"));

        if (!confirm)
            return;

        try
        {
            var success = await _linkService.DeleteCategoryAsync(category.Id);
            if (!success)
            {
                await page.DisplayAlertAsync(L("Error"), L("CouldNotDeleteCategory"), "OK");
                return;
            }

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync(L("Error"), F("CouldNotDeleteCategoryDetails", ex.Message), "OK");
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await CloseAsync();
    }

    private async Task CloseAsync()
    {
        var page = Shell.Current?.CurrentPage;
        if (page is null)
            return;

        await page.ClosePopupAsync();
    }

    private static string L(string key) => LocalizationManager.Instance.Get(key);
    private static string F(string key, params object[] args) => LocalizationManager.Instance.Format(key, args);
}
