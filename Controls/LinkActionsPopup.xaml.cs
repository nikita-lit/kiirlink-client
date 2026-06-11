using KiirLink.Services;

namespace KiirLink.Controls;

public enum LinkActionsPopupAction
{
    OpenOriginal,
    CopyShort,
    CreateQRCode,
    CopyOriginal,
    ViewAnalytics,
    AssignCategory,
    ToggleFavourite,
    Delete
}

public partial class LinkActionsPopup
{
    public LinkActionsPopup( bool isFavourite )
    {
        InitializeComponent();
        CategoryButton.Text = LocalizationManager.Instance.Get("AssignCategory");
        FavouriteButton.Text = LocalizationManager.Instance.Get(
            isFavourite ? "RemoveFromFavourites" : "AddToFavourites");
    }

    private async void OnActionClicked(object? sender, EventArgs e)
    {
        if (Enum.TryParse<LinkActionsPopupAction>(
                ((Button)sender!).CommandParameter?.ToString(),
                out var action))
            await UiHelpers.ClosePopupAsync(action);
    }
}
