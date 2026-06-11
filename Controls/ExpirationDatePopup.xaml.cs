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

    private async void OnSelectionClicked(object? sender, EventArgs e) =>
        await UiHelpers.ClosePopupAsync(((Button)sender!).CommandParameter switch
        {
            "Never" => new ExpirationDateSelection(true, null),
            "Save" => new ExpirationDateSelection(false, ExpirationPicker.Date),
            _ => new ExpirationDateSelection(false, null)
        });
}
