using CommunityToolkit.Maui.Extensions;

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
        CategoryButton.Text = "Assign category";
        FavouriteButton.Text = isFavourite ? "Remove from favourites" : "Add to favourites";
    }

    private async Task CloseWith( LinkActionsPopupAction action )
    {
        var page = Shell.Current?.CurrentPage;
        if ( page is null )
            return;

        await page.ClosePopupAsync( action );
    }
    
    private async void OnCopyShortClicked( object? sender, EventArgs e ) =>
        await CloseWith( LinkActionsPopupAction.CopyShort );

    private async void OnCopyOriginalClicked( object? sender, EventArgs e ) =>
        await CloseWith( LinkActionsPopupAction.CopyOriginal );
    
    private async void OnCreateQRCodeClicked( object? sender, EventArgs e ) =>
        await CloseWith( LinkActionsPopupAction.CreateQRCode );

    private async void OnAnalyticsClicked( object? sender, EventArgs e ) =>
        await CloseWith( LinkActionsPopupAction.ViewAnalytics );

    private async void OnAssignCategoryClicked( object? sender, EventArgs e ) =>
        await CloseWith( LinkActionsPopupAction.AssignCategory );
    
    private async void OnFavouriteClicked( object? sender, EventArgs e ) =>
        await CloseWith( LinkActionsPopupAction.ToggleFavourite );

    private async void OnDeleteClicked( object? sender, EventArgs e ) =>
        await CloseWith( LinkActionsPopupAction.Delete );
}
