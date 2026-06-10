using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Tests;

public sealed class ServiceTests
{
    [Fact]
    public async Task AuthService_ForwardsLoginAndProfileCalls()
    {
        var api = new FakeApiClient
        {
            LoginResult = (true, null),
            Profile = new InfoResponse { Email = "user@example.com" }
        };
        var service = new AuthService(api);

        Assert.True((await service.LoginAsync("user@example.com", "secret")).Success);
        Assert.Equal("user@example.com", (await service.GetProfileAsync())!.Email);
        Assert.Equal(1, api.LoginCalls);

        Assert.True((await service.ChangePasswordAsync("secret", "new-secret")).Success);
        Assert.Equal(("secret", "new-secret"), api.LastPasswordChange);
    }

    [Fact]
    public async Task LinkService_ForwardsPagingAndCategoryArguments()
    {
        var api = new FakeApiClient
        {
            LinksPage = new PaginatedLinksResponse
            {
                Items = [new LinkModel { Id = 7 }],
                TotalCount = 1
            }
        };
        var service = new LinkService(api);

        var page = await service.GetLinksPageAsync(2, 5, 9);

        Assert.Single(page.Items);
        Assert.Equal((2, 5, 9), api.LastPageRequest);
    }

    private sealed class FakeApiClient : IApiClient
    {
        public (bool Success, string? Error) LoginResult { get; set; }
        public InfoResponse? Profile { get; set; }
        public PaginatedLinksResponse LinksPage { get; set; } = new();
        public int LoginCalls { get; private set; }
        public (string OldPassword, string NewPassword) LastPasswordChange { get; private set; }
        public (int Page, int Limit, int? CategoryId) LastPageRequest { get; private set; }

        public Task<bool> HasStoredTokensAsync() => Task.FromResult(true);
        public Task ClearTokensAsync() => Task.CompletedTask;

        public Task<(bool Success, string? Error)> RegisterAsync(string email, string password,
            CancellationToken ct = default) => Task.FromResult((true, (string?)null));

        public Task<(bool Success, string? Error)> LoginAsync(string email, string password,
            CancellationToken ct = default)
        {
            LoginCalls++;
            return Task.FromResult(LoginResult);
        }

        public Task<InfoResponse?> GetProfileAsync(CancellationToken ct = default) => Task.FromResult(Profile);

        public Task<(bool Success, string? Error)> ChangePasswordAsync(string oldPassword, string newPassword,
            CancellationToken ct = default)
        {
            LastPasswordChange = (oldPassword, newPassword);
            return Task.FromResult((true, (string?)null));
        }

        public Task<PaginatedLinksResponse> GetLinksPageAsync(int page = 1, int limit = 20,
            int? categoryId = null, CancellationToken ct = default)
        {
            LastPageRequest = (page, limit, categoryId);
            return Task.FromResult(LinksPage);
        }

        public Task<List<LinkModel>> GetLinksAsync(int page = 1, int limit = 20, int? categoryId = null,
            CancellationToken ct = default) => Task.FromResult(LinksPage.Items);

        public Task<(bool Success, string? Error)> ShortenLinkAsync(string originalUrl,
            DateTime? expiresAt = null, bool isPublic = true, CancellationToken ct = default)
            => Task.FromResult((true, (string?)null));

        public Task<bool> RemoveLinkAsync(int linkId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<LinkStatsModel?> GetLinkStatsAsync(int id, CancellationToken ct = default)
            => Task.FromResult<LinkStatsModel?>(null);
        public Task<List<LinkActivityModel>> GetLinkActivityAsync(int id, CancellationToken ct = default)
            => Task.FromResult<List<LinkActivityModel>>([]);
        public Task<List<LinkModel>> GetFavouritesAsync(CancellationToken ct = default)
            => Task.FromResult<List<LinkModel>>([]);
        public Task<bool> AddFavouriteAsync(int linkId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> RemoveFavouriteAsync(int linkId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<List<CategoryModel>> GetCategoriesAsync(CancellationToken ct = default)
            => Task.FromResult<List<CategoryModel>>([]);
        public Task<CategoryModel?> CreateCategoryAsync(string categoryName, CancellationToken ct = default)
            => Task.FromResult<CategoryModel?>(new CategoryModel { Name = categoryName });
        public Task<bool> AddCategoryAsync(string categoryName, CancellationToken ct = default)
            => Task.FromResult(true);
        public Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> AssignCategoryAsync(int linkId, int? categoryId, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}
