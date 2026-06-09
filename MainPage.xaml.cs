namespace KiirLink;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        var slug = CustomLinkEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(slug))
        {
            await DisplayAlertAsync("Add a custom link", "Enter the short name you want to use.", "OK");
            return;
        }

        await DisplayAlertAsync("Link ready", $"kiirlink.ee/{slug}", "Done");
    }
}
