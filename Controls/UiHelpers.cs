using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Behaviors;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Controls;

internal static class UiHelpers
{
    public static PopupOptions PlainPopup() => new() { Shape = null, Shadow = null };

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
