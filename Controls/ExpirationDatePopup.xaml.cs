using CommunityToolkit.Maui.Extensions;

namespace KiirLink.Controls;

public sealed record ExpirationDateSelection(bool IsNever, DateTime? Date);

public partial class ExpirationDatePopup
{
    public ExpirationDatePopup(DateTime? selectedDate)
    {
        InitializeComponent();
        ExpirationPicker.MinimumDate = DateTime.Today.AddDays(1);
        ExpirationPicker.Date = selectedDate is { } date && date.Date >= ExpirationPicker.MinimumDate
            ? date.Date
            : ExpirationPicker.MinimumDate;
    }

    private async void OnNeverClicked(object? sender, EventArgs e)
    {
        await CloseAsync(new ExpirationDateSelection(true, null));
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(new ExpirationDateSelection(false, null));
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        await CloseAsync(new ExpirationDateSelection(false, ExpirationPicker.Date));
    }

    private static async Task CloseAsync(ExpirationDateSelection result)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is not null)
            await page.ClosePopupAsync(result);
    }
}
