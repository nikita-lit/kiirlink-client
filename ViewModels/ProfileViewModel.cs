using KiirLink.Services;

namespace KiirLink.ViewModels;

public sealed class ProfileViewModel : ViewModelBase
{
    private readonly ApiClient _api;
    private readonly INavigationService _navigation;
    private string _displayName = "KiirLink user";
    private string _email = string.Empty;

    public ProfileViewModel(ApiClient api, IConnectivityService connectivity, INavigationService navigation)
        : base(connectivity)
    {
        _api = api;
        _navigation = navigation;
    }

    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public string Email
    {
        get => _email;
        private set => SetProperty(ref _email, value);
    }

    public async Task LoadAsync()
    {
        if (!IsOnline)
            return;

        await RunBusyAsync(async () =>
        {
            var profile = await _api.GetProfileAsync();
            if (profile is null)
                return;

            Email = profile.Email;
            DisplayName = profile.Email.Split('@')[0];
        });
    }

    public async Task LogoutAsync()
    {
        await _api.ClearTokensAsync();
        await _navigation.GoToAsync("//SignIn");
    }
}
