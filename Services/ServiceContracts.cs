using KiirLink.Models;

namespace KiirLink.Services;

public interface IApiClient
{
    Task<bool> HasStoredTokensAsync();
    Task ClearTokensAsync();
    Task<(bool Success, string? Error)> RegisterAsync(string email, string password, CancellationToken ct = default);
    Task<(bool Success, string? Error)> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<(bool Success, string? Error)> ChangePasswordAsync(string oldPassword, string newPassword,
        CancellationToken ct = default);
    Task<InfoResponse?> GetProfileAsync(CancellationToken ct = default);
    Task<PaginatedLinksResponse> GetLinksPageAsync(int page = 1, int limit = 20,
        int? categoryId = null, CancellationToken ct = default);
    Task<List<LinkModel>> GetLinksAsync(int page = 1, int limit = 20, int? categoryId = null,
        CancellationToken ct = default);
    Task<(bool Success, string? Error)> ShortenLinkAsync(string originalUrl, DateTime? expiresAt = null,
        bool isPublic = true, CancellationToken ct = default);
    Task<bool> RemoveLinkAsync(int linkId, CancellationToken ct = default);
    Task<LinkStatsModel?> GetLinkStatsAsync(int id, CancellationToken ct = default);
    Task<List<LinkActivityModel>> GetLinkActivityAsync(int id, CancellationToken ct = default);
    Task<List<LinkModel>> GetFavouritesAsync(CancellationToken ct = default);
    Task<bool> AddFavouriteAsync(int linkId, CancellationToken ct = default);
    Task<bool> RemoveFavouriteAsync(int linkId, CancellationToken ct = default);
    Task<List<CategoryModel>> GetCategoriesAsync(CancellationToken ct = default);
    Task<CategoryModel?> CreateCategoryAsync(string categoryName, CancellationToken ct = default);
    Task<bool> AddCategoryAsync(string categoryName, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default);
    Task<bool> AssignCategoryAsync(int linkId, int? categoryId, CancellationToken ct = default);
}

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<(bool Success, string? Error)> LoginAsync(string email, string password);
    Task<(bool Success, string? Error)> RegisterAsync(string email, string password);
    Task<(bool Success, string? Error)> ChangePasswordAsync(string oldPassword, string newPassword);
    Task<InfoResponse?> GetProfileAsync();
    Task LogoutAsync();
}

public interface ILinkService
{
    Task<PaginatedLinksResponse> GetLinksPageAsync(int page = 1, int limit = 20,
        int? categoryId = null);
    Task<List<LinkModel>> GetLinksAsync(int page = 1, int limit = 20, int? categoryId = null);
    Task<(bool Success, string? Error)> ShortenLinkAsync(string originalUrl, DateTime? expiresAt = null,
        bool isPublic = true);
    Task<bool> RemoveLinkAsync(int linkId);
    Task<LinkStatsModel?> GetLinkStatsAsync(int id);
    Task<List<LinkActivityModel>> GetLinkActivityAsync(int id);
    Task<List<LinkModel>> GetFavouritesAsync();
    Task<bool> AddFavouriteAsync(int linkId);
    Task<bool> RemoveFavouriteAsync(int linkId);
    Task<List<CategoryModel>> GetCategoriesAsync();
    Task<CategoryModel?> CreateCategoryAsync(string name);
    Task<bool> AddCategoryAsync(string name);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> AssignCategoryAsync(int linkId, int? categoryId);
}

public interface IConnectivityService
{
    bool IsOnline { get; }
    event EventHandler<bool>? ConnectivityChanged;
}

public interface INavigationService
{
    Task GoToAsync(string route);
}

public interface IDialogService
{
    Task AlertAsync(string title, string message, string cancel = "OK");
}
