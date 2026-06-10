using CommunityToolkit.Maui.Extensions;
using KiirLink.Services;

namespace KiirLink.Controls;

public partial class ChangePasswordPopup
{
    private readonly IAuthService _authService;

    public ChangePasswordPopup(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await CloseAsync(false);
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var currentPassword = CurrentPasswordEntry.Text ?? string.Empty;
        var newPassword = NewPasswordEntry.Text ?? string.Empty;
        var confirmation = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            ShowError("Enter your current password.");
            return;
        }

        if (newPassword.Length < 6)
        {
            ShowError("The new password must contain at least 6 characters.");
            return;
        }

        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
        {
            ShowError("The new passwords do not match.");
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _authService.ChangePasswordAsync(currentPassword, newPassword);
            if (!result.Success)
            {
                ShowError(result.Error ?? "Could not change the password.");
                return;
            }

            await CloseAsync(true);
        }
        catch (Exception ex)
        {
            ShowError($"Could not change password: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }

    private void SetBusy(bool busy)
    {
        SaveButton.IsEnabled = !busy;
        LoadingIndicator.IsRunning = busy;
        LoadingIndicator.IsVisible = busy;
    }

    private static async Task CloseAsync(bool changed)
    {
        var page = Shell.Current?.CurrentPage;
        if (page is not null)
            await page.ClosePopupAsync(changed);
    }
}
