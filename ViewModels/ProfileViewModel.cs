using KiirLink.Services;

namespace KiirLink.ViewModels;

public sealed class ProfileViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly INavigationService _navigation;
    private string _displayName = "KiirLink user";
    private string _email = string.Empty;

    public ProfileViewModel(IAuthService auth, IConnectivityService connectivity, INavigationService navigation)
        : base(connectivity)
    {
        _auth = auth;
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
            var profile = await _auth.GetProfileAsync();
            if (profile is null)
                return;

            Email = profile.Email;
            DisplayName = profile.Email.Split('@')[0];
        });
    }

    public async Task LogoutAsync()
    {
        await _auth.LogoutAsync();
        await _navigation.GoToAsync("//SignIn");
    }
}
