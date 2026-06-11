using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private readonly ILinkService _links;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly LinkPreferencesService _preferences;
    private string _originalUrl = string.Empty;

    public HomeViewModel(ILinkService links, IConnectivityService connectivity, INavigationService navigation,
        IDialogService dialogs, LinkPreferencesService preferences) : base(connectivity)
    {
        _links = links;
        _navigation = navigation;
        _dialogs = dialogs;
        _preferences = preferences;
        CreateLinkCommand = new AsyncCommand(CreateLinkAsync, () => CanInteract);
        TrackCanInteract(CreateLinkCommand);
    }

    public string OriginalUrl
    {
        get => _originalUrl;
        set => SetProperty(ref _originalUrl, value);
    }

    public AsyncCommand CreateLinkCommand { get; }

    private async Task CreateLinkAsync()
    {
        var value = OriginalUrl.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            await _dialogs.AlertAsync(L("AddLink"), L("EnterUrl"));
            return;
        }

        var originalUrl = value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"https://{value}";

        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
        {
            await _dialogs.AlertAsync(L("InvalidLink"), L("EnterValidWebAddress"));
            return;
        }

        await RunBusyAsync(async () =>
        {
            var expiresAt = LinkExpiration.EndOfSelectedDayUtc(_preferences.AutoExpirationDate);
            var result = await _links.ShortenLinkAsync(
                originalUrl,
                expiresAt,
                _preferences.IsPublic,
                _preferences.DefaultCategoryId);
            if (!result.Success)
            {
                await _dialogs.AlertAsync(L("CouldNotCreateLink"), result.Error ?? L("TryAgain"));
                return;
            }

            OriginalUrl = string.Empty;
            await _dialogs.AlertAsync(L("LinkCreated"), L("ShortLinkReady"), L("Done"));
            await _navigation.GoToAsync("//Links");
        });

        if (HasError)
            await _dialogs.AlertAsync(L("NetworkError"), ErrorMessage!);
    }

    private static string L(string key) => LocalizationManager.Instance.Get(key);
}
