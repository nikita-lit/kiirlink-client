using KiirLink.Services;

namespace KiirLink.Controls;

public partial class ChangePasswordPopup
{
    private readonly ApiClient _api;

    public ChangePasswordPopup(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await UiHelpers.ClosePopupAsync(false);

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var currentPassword = CurrentPasswordEntry.Text ?? string.Empty;
        var newPassword = NewPasswordEntry.Text ?? string.Empty;
        var confirmation = ConfirmPasswordEntry.Text ?? string.Empty;

        var errorKey = string.IsNullOrWhiteSpace(currentPassword)
            ? "EnterCurrentPassword"
            : newPassword.Length < 6
                ? "PasswordMinimumLength"
                : newPassword != confirmation
                    ? "PasswordsDoNotMatch"
                    : null;
        if (errorKey is not null)
        {
            ShowError(LocalizationManager.Instance.Get(errorKey));
            return;
        }

        SetBusy(true);
        try
        {
            var result = await _api.ChangePasswordAsync(currentPassword, newPassword);
            if (!result.Success)
            {
                ShowError(LocalizationManager.Instance.LocalizeAuthError(
                    result.Error ?? LocalizationManager.Instance.Get("CouldNotChangePassword")));
                return;
            }

            await UiHelpers.ClosePopupAsync(true);
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

}
