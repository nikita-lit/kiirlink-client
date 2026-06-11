using KiirLink.Models;

namespace KiirLink.Services;

/// <summary>
/// High-level auth operations built on top of <see cref="ApiClient"/>.
/// </summary>
public class AuthService(IApiClient api) : IAuthService
{
    public Task<bool> IsAuthenticatedAsync() => api.HasStoredTokensAsync();

    public Task<(bool Success, string? Error)> LoginAsync(string email, string password) =>
        api.LoginAsync(email, password);

    public Task<(bool Success, string? Error)> RegisterAsync(string email, string password) =>
        api.RegisterAsync(email, password);

    public Task<(bool Success, string? Error)> ChangePasswordAsync(string oldPassword, string newPassword) =>
        api.ChangePasswordAsync(oldPassword, newPassword);

    public Task<InfoResponse?> GetProfileAsync() => api.GetProfileAsync();

    public Task LogoutAsync() => api.ClearTokensAsync();
}
