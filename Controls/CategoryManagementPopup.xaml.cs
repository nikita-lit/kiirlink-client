using System.Collections.ObjectModel;
using KiirLink.Extensions;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Controls;

public partial class CategoryManagementPopup
{
    private readonly ILinkService _linkService;
    private readonly IConnectivityService _connectivity;
    private readonly ObservableCollection<CategoryModel> _categories = [];
    private bool _loaded;

    public ObservableCollection<CategoryModel> Categories => _categories;

    public CategoryManagementPopup(ILinkService linkService, IConnectivityService connectivity)
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
            await Page.DisplayAlertAsync(
                L("Offline"),
                L("ConnectToManageCategories"),
                "OK");
            await UiHelpers.ClosePopupAsync();
            return;
        }

        try
        {
            _categories.ReplaceWith(await _linkService.GetCategoriesAsync());
        }
        catch (Exception ex)
        {
            await Page.DisplayAlertAsync(
                L("Error"),
                F("CouldNotLoadCategories", ex.Message),
                "OK");
            await UiHelpers.ClosePopupAsync();
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
                await Page.DisplayAlertAsync(
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
            await Page.DisplayAlertAsync(
                L("Error"),
                F("CouldNotCreateCategoryDetails", ex.Message),
                "OK");
        }
    }

    private async void OnDeleteCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not CategoryModel category)
            return;

        var confirm = await Page.DisplayAlertAsync(
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
                await Page.DisplayAlertAsync(L("Error"), L("CouldNotDeleteCategory"), "OK");
                return;
            }

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            await Page.DisplayAlertAsync(L("Error"), F("CouldNotDeleteCategoryDetails", ex.Message), "OK");
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e) =>
        await UiHelpers.ClosePopupAsync();

    private static Page Page => UiHelpers.CurrentPage!;
    private static string L(string key) => LocalizationManager.Instance.Get(key);
    private static string F(string key, params object[] args) => LocalizationManager.Instance.Format(key, args);
}
