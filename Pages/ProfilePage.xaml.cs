namespace KiirLink.Pages;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    private async void OnEditProfileClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Profile", "Profile editing is ready to connect to your account data.", "OK");
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var shouldLogout = await DisplayAlertAsync("Log out", "Are you sure you want to log out?", "Log out", "Cancel");

        if (shouldLogout)
        {
            await Shell.Current.GoToAsync("//Home");
        }
    }
}
