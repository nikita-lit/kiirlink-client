using KiirLink.Models;

namespace KiirLink.Services;

/// <summary>
/// High-level auth operations built on top of <see cref="ApiClient"/>.
/// </summary>
public class AuthService(ApiClient api)
{
    public bool IsAuthenticated => ApiClient.IsAuthenticated;

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
        => await api.LoginAsync(email, password);

    public async Task<(bool Success, string? Error)> RegisterAsync(string email, string password)
        => await api.RegisterAsync(email, password);

    public async Task<InfoResponse?> GetProfileAsync()
        => await api.GetProfileAsync();

    public void Logout() => ApiClient.ClearTokens();
}
