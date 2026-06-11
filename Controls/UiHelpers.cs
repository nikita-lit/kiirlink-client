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

    public static async Task<CategoryModel?> AssignCategoryAsync(
        Page page,
        ILinkService links,
        int linkId,
        IReadOnlyCollection<CategoryModel> categories)
    {
        var localization = LocalizationManager.Instance;
        if (categories.Count == 0)
        {
            await page.DisplayAlertAsync(
                localization.Get("NoCategories"),
                localization.Get("CreateCategoryFirst"),
                "OK");
            return null;
        }

        var cancel = localization.Get("Cancel");
        var action = await page.DisplayActionSheetAsync(
            localization.Get("AssignCategory"),
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
            await page.DisplayAlertAsync(
                localization.Get("Error"),
                localization.Get("CouldNotAssignCategory"),
                "OK");
            return null;
        }

        await page.DisplayAlertAsync(
            localization.Get("CategoryAssigned"),
            localization.Format("CategoryAssignedMessage", category.Name),
            "OK");
        return category;
    }

    public static Task ShowQrCodeAsync(Page page, LinkModel link) =>
        page.ShowPopupAsync(
            new QRCodePopup($"{AppHostHelper.BaseUrl}/{link.ShortUrl}"),
            PlainPopup());

    public static async Task<bool> DeleteLinkAsync(Page page, ILinkService links, int linkId, string title)
    {
        var localization = LocalizationManager.Instance;
        if (!await page.DisplayAlertAsync(
                localization.Get("DeleteLink"),
                localization.Format("DeleteLinkConfirmation", title),
                localization.Get("Delete"),
                localization.Get("Cancel")))
            return false;

        try
        {
            if (await links.RemoveLinkAsync(linkId))
                return true;

            await page.DisplayAlertAsync(
                localization.Get("Error"),
                localization.Get("CouldNotDeleteLink"),
                "OK");
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync(
                localization.Get("Error"),
                localization.Format("ErrorDetails", ex.Message),
                "OK");
        }

        return false;
    }

    public static async Task<bool> SetFavouriteAsync(
        Page page,
        ILinkService links,
        int linkId,
        bool favourite)
    {
        var localization = LocalizationManager.Instance;
        try
        {
            var success = favourite
                ? await links.AddFavouriteAsync(linkId)
                : await links.RemoveFavouriteAsync(linkId);
            if (success)
                return true;

            await page.DisplayAlertAsync(
                localization.Get("Error"),
                localization.Get("CouldNotUpdateFavourite"),
                "OK");
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync(
                localization.Get("Error"),
                localization.Format("ErrorDetails", ex.Message),
                "OK");
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
