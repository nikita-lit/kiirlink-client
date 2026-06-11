using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Extensions;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Controls;

internal static class UiHelpers
{
    public static Page? CurrentPage => Shell.Current?.CurrentPage;

    public static PopupOptions PlainPopup() => new() { Shape = null, Shadow = null };

    public static Task ClosePopupAsync() =>
        CurrentPage?.ClosePopupAsync() ?? Task.CompletedTask;

    public static Task ClosePopupAsync<T>(T result) =>
        CurrentPage?.ClosePopupAsync(result) ?? Task.CompletedTask;

    private static Task AlertAsync(Page page, string title, string message) =>
        page.DisplayAlertAsync(title, message, "OK");

    private static Task ErrorAsync(Page page, string key, params object[] args) =>
        AlertAsync(
            page,
            L("Error"),
            args.Length == 0 ? L(key) : LocalizationManager.Instance.Format(key, args));

    private static string L(string key) => LocalizationManager.Instance.Get(key);

    public static async Task<CategoryModel?> AssignCategoryAsync(
        Page page,
        ILinkService links,
        int linkId,
        IReadOnlyCollection<CategoryModel> categories)
    {
        if (categories.Count == 0)
        {
            await AlertAsync(page, L("NoCategories"), L("CreateCategoryFirst"));
            return null;
        }

        var cancel = L("Cancel");
        var action = await page.DisplayActionSheetAsync(
            L("AssignCategory"),
            cancel,
            null,
            categories.Select(category => category.Name).ToArray());
        var category = string.IsNullOrWhiteSpace(action) || action == cancel
            ? null
            : categories.FirstOrDefault(item =>
                string.Equals(item.Name, action, StringComparison.OrdinalIgnoreCase));
        if (category is null)
            return null;

        if (!await links.AssignCategoryAsync(linkId, category.Id))
        {
            await ErrorAsync(page, "CouldNotAssignCategory");
            return null;
        }

        await AlertAsync(
            page,
            L("CategoryAssigned"),
            LocalizationManager.Instance.Format("CategoryAssignedMessage", category.Name));
        return category;
    }

    public static Task ShowQrCodeAsync(Page page, LinkModel link) =>
        page.ShowPopupAsync(
            new QRCodePopup($"{AppHostHelper.BaseUrl}/{link.ShortUrl}"),
            PlainPopup());

    public static async Task<bool> DeleteLinkAsync(Page page, ILinkService links, int linkId, string title)
    {
        if (!await page.DisplayAlertAsync(
                L("DeleteLink"),
                LocalizationManager.Instance.Format("DeleteLinkConfirmation", title),
                L("Delete"),
                L("Cancel")))
            return false;

        try
        {
            if (await links.RemoveLinkAsync(linkId))
                return true;

            await ErrorAsync(page, "CouldNotDeleteLink");
        }
        catch (Exception ex)
        {
            await ErrorAsync(page, "ErrorDetails", ex.Message);
        }

        return false;
    }

    public static async Task<bool> SetFavouriteAsync(
        Page page,
        ILinkService links,
        int linkId,
        bool favourite)
    {
        try
        {
            var success = favourite
                ? await links.AddFavouriteAsync(linkId)
                : await links.RemoveFavouriteAsync(linkId);
            if (success)
                return true;

            await ErrorAsync(page, "CouldNotUpdateFavourite");
        }
        catch (Exception ex)
        {
            await ErrorAsync(page, "ErrorDetails", ex.Message);
        }

        return false;
    }

    public static void SetTint(this Image image, Color tint)
    {
        var behavior = image.Behaviors.OfType<IconTintColorBehavior>().FirstOrDefault();
        if (behavior is null)
        {
            behavior = new IconTintColorBehavior();
            image.Behaviors.Add(behavior);
        }

        behavior.TintColor = tint;
    }
}
