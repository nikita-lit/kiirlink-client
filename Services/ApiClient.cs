using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KiirLink.Models;

namespace KiirLink.Services;

public sealed class ApiClient(HttpClient http, IConnectivityService connectivity)
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static bool _tokensLoaded;
    private static string? _accessToken;
    private static string? _refreshToken;

    public async Task<bool> HasStoredTokensAsync()
    {
        await LoadTokensAsync();
        return _accessToken is not null;
    }

    public async Task ClearTokensAsync()
    {
        await LoadTokensAsync();
        _accessToken = _refreshToken = null;

        try
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
        }
        catch
        {
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        using var response = await SendRawAsync(
            () => JsonRequest(HttpMethod.Post, "/api/auth/register",
                new CredentialsRequest { Email = email, Password = password }),
            ct);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await AuthErrorAsync(response, AuthOperation.Register, ct));
    }

    public async Task<(bool Success, string? Error)> LoginAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        using var response = await SendRawAsync(
            () => JsonRequest(HttpMethod.Post, "/api/auth/login",
                new CredentialsRequest { Email = email, Password = password }),
            ct);

        if (!response.IsSuccessStatusCode)
            return (false, await AuthErrorAsync(response, AuthOperation.Login, ct));

        var tokens = await ReadAsync<AccessTokenResponse>(response, ct);
        if (tokens is null)
            return (false, "Invalid response from server.");

        await SaveTokensAsync(tokens);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        string oldPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        using var response = await SendAsync(
            () => JsonRequest(HttpMethod.Post, "/api/auth/manage/info",
                new ChangePasswordRequest { OldPassword = oldPassword, NewPassword = newPassword }),
            ct);

        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, await AuthErrorAsync(response, AuthOperation.ChangePassword, ct));
    }

    public Task<InfoResponse?> GetProfileAsync(CancellationToken ct = default) =>
        GetAsync<InfoResponse>("/api/auth/manage/info", ct);

    public async Task<PaginatedLinksResponse> GetLinksPageAsync(
        int page = 1,
        int limit = 20,
        int? categoryId = null,
        CancellationToken ct = default) =>
        await GetAsync<PaginatedLinksResponse>(
            UriQueryBuilder.Build(
                "/api/links/get",
                ("page", page),
                ("limit", limit),
                ("categoryId", categoryId)),
            ct) ?? new();

    public async Task<List<LinkModel>> GetLinksAsync(
        int page = 1,
        int limit = 20,
        int? categoryId = null,
        CancellationToken ct = default) =>
        (await GetLinksPageAsync(page, limit, categoryId, ct)).Items;

    public async Task<LinkCreationResult> ShortenLinkAsync(
        string originalUrl,
        DateTime? expiresAt = null,
        bool isPublic = true,
        int? categoryId = null,
        CancellationToken ct = default)
    {
        var path = UriQueryBuilder.Build(
            "/api/links/shorten",
            ("originalUrl", originalUrl),
            ("isPublic", isPublic),
            ("expiresAt", expiresAt),
            ("categoryId", categoryId));
        using var response = await SendAsync(() => new(HttpMethod.Post, path), ct);

        if (response.IsSuccessStatusCode)
            return new()
            {
                Success = true,
                ShortUrl = (await ReadAsync<CreatedLinkResponse>(response, ct))?.ShortUrl
            };

        return new()
        {
            Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: " +
                    await response.Content.ReadAsStringAsync(ct)
        };
    }

    public Task<bool> RemoveLinkAsync(int linkId, CancellationToken ct = default) =>
        SuccessAsync(HttpMethod.Post, Query("/api/links/remove", "linkId", linkId), ct);

    public Task<LinkStatsModel?> GetLinkStatsAsync(int id, CancellationToken ct = default) =>
        GetAsync<LinkStatsModel>($"/api/links/{id}/stats", ct);

    public async Task<List<LinkActivityModel>> GetLinkActivityAsync(
        int id,
        CancellationToken ct = default) =>
        await GetAsync<List<LinkActivityModel>>($"/api/links/{id}/activity", ct) ?? [];

    public async Task<List<LinkModel>> GetFavouritesAsync(CancellationToken ct = default) =>
        await GetAsync<List<LinkModel>>("/api/links/favourites", ct) ?? [];

    public Task<bool> AddFavouriteAsync(int linkId, CancellationToken ct = default) =>
        SuccessAsync(HttpMethod.Post, Query("/api/links/favourite", "linkId", linkId), ct);

    public Task<bool> RemoveFavouriteAsync(int linkId, CancellationToken ct = default) =>
        SuccessAsync(HttpMethod.Post, Query("/api/links/unfavourite", "linkId", linkId), ct);

    public async Task<List<CategoryModel>> GetCategoriesAsync(CancellationToken ct = default) =>
        await GetAsync<List<CategoryModel>>("/api/links/categories", ct) ?? [];

    public Task<CategoryModel?> CreateCategoryAsync(
        string name,
        CancellationToken ct = default) =>
        ResultAsync<CategoryModel>(
            HttpMethod.Post,
            Query("/api/links/category", "categoryName", name),
            ct);

    public async Task<bool> AddCategoryAsync(string name, CancellationToken ct = default) =>
        await CreateCategoryAsync(name, ct) is not null;

    public Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default) =>
        SuccessAsync(HttpMethod.Delete, $"/api/links/category/{id}", ct);

    public Task<bool> AssignCategoryAsync(
        int linkId,
        int? categoryId,
        CancellationToken ct = default) =>
        SuccessAsync(
            HttpMethod.Put,
            Query($"/api/links/{linkId}/category", "categoryId", categoryId),
            ct);

    private static string Query(string path, string key, object? value) =>
        UriQueryBuilder.Build(path, (key, value));

    private Task<T?> GetAsync<T>(string path, CancellationToken ct) =>
        ResultAsync<T>(HttpMethod.Get, path, ct);

    private async Task<T?> ResultAsync<T>(HttpMethod method, string path, CancellationToken ct)
    {
        using var response = await SendAsync(() => new(method, path), ct);
        return response.IsSuccessStatusCode ? await ReadAsync<T>(response, ct) : default;
    }

    private async Task<bool> SuccessAsync(HttpMethod method, string path, CancellationToken ct)
    {
        using var response = await SendAsync(() => new(method, path), ct);
        return response.IsSuccessStatusCode;
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> createRequest,
        CancellationToken ct)
    {
        await LoadTokensAsync();
        var response = await SendRawAsync(() => Authorized(createRequest()), ct);

        if (response.StatusCode != HttpStatusCode.Unauthorized ||
            _refreshToken is null ||
            !await RefreshTokensAsync(ct))
            return response;

        response.Dispose();
        return await SendRawAsync(() => Authorized(createRequest()), ct);
    }

    private HttpRequestMessage Authorized(HttpRequestMessage request)
    {
        if (_accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return request;
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        Func<HttpRequestMessage> createRequest,
        CancellationToken ct)
    {
        if (!connectivity.IsOnline)
            throw new NetworkUnavailableException();

        try
        {
            using var request = createRequest();
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiException("The server took too long to respond. Please try again.", null, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new NetworkUnavailableException(ex);
        }
    }

    private async Task<bool> RefreshTokensAsync(CancellationToken ct)
    {
        try
        {
            using var response = await SendRawAsync(
                () => JsonRequest(
                    HttpMethod.Post,
                    "/api/auth/refresh",
                    new RefreshRequest { RefreshToken = _refreshToken! }),
                ct);
            var tokens = response.IsSuccessStatusCode
                ? await ReadAsync<AccessTokenResponse>(response, ct)
                : null;

            if (tokens is null)
            {
                await ClearTokensAsync();
                return false;
            }

            await SaveTokensAsync(tokens);
            return true;
        }
        catch
        {
            await ClearTokensAsync();
            return false;
        }
    }

    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object body) =>
        new(method, path)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static async Task<string> AuthErrorAsync(
        HttpResponseMessage response,
        AuthOperation operation,
        CancellationToken ct) =>
        AuthErrorMessages.FromResponse(
            response.StatusCode,
            await response.Content.ReadAsStringAsync(ct),
            operation);

    private static async Task LoadTokensAsync()
    {
        if (_tokensLoaded)
            return;

        await TokenLock.WaitAsync();
        try
        {
            if (_tokensLoaded)
                return;

            try
            {
                _accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
                _refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            }
            catch
            {
                _accessToken = _refreshToken = null;
            }

            _tokensLoaded = true;
        }
        finally
        {
            TokenLock.Release();
        }
    }

    private static async Task SaveTokensAsync(AccessTokenResponse tokens)
    {
        _accessToken = tokens.AccessToken;
        _refreshToken = tokens.RefreshToken;

        try
        {
            await SecureStorage.Default.SetAsync(AccessTokenKey, tokens.AccessToken);
            await SecureStorage.Default.SetAsync(RefreshTokenKey, tokens.RefreshToken);
        }
        catch
        {
        }
    }
}
