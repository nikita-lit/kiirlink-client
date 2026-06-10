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
            ShowError(LocalizationManager.Instance.Get("EnterCurrentPassword"));
            return;
        }

        if (newPassword.Length < 6)
        {
            ShowError(LocalizationManager.Instance.Get("PasswordMinimumLength"));
            return;
        }

        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
        {
            ShowError(LocalizationManager.Instance.Get("PasswordsDoNotMatch"));
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _authService.ChangePasswordAsync(currentPassword, newPassword);
            if (!result.Success)
            {
                ShowError(LocalizationManager.Instance.LocalizeAuthError(
                    result.Error ?? LocalizationManager.Instance.Get("CouldNotChangePassword")));
                return;
            }

            await CloseAsync(true);
        }
        catch (Exception ex)
        {
            ShowError(LocalizationManager.Instance.Format("CouldNotChangePasswordDetails", ex.Message));
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
