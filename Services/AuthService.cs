using KiirLink.Models;

namespace KiirLink.Services;

/// <summary>
/// High-level auth operations built on top of <see cref="ApiClient"/>.
/// </summary>
public class AuthService(IApiClient api) : IAuthService
{
    public Task<bool> IsAuthenticatedAsync() => api.HasStoredTokensAsync();

    public async Task<(bool Success, string? Error)> LoginAsync( string email, string password )
        => await api.LoginAsync( email, password );

    public async Task<(bool Success, string? Error)> RegisterAsync( string email, string password )
        => await api.RegisterAsync( email, password );

    public Task<(bool Success, string? Error)> ChangePasswordAsync( string oldPassword, string newPassword )
        => api.ChangePasswordAsync( oldPassword, newPassword );

    public async Task<InfoResponse?> GetProfileAsync()
        => await api.GetProfileAsync();

    public Task LogoutAsync() => api.ClearTokensAsync();
}
