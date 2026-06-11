using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KiirLink.Models;

namespace KiirLink.Services;

/// <summary>
/// Low-level HTTP client for the KiirLink API.
/// Handles token injection, automatic refresh, and JSON serialization.
/// </summary>
public class ApiClient : IApiClient
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly SemaphoreSlim TokenLock = new(1, 1);
    private static bool _tokensLoaded;
    private static string? _accessToken;
    private static string? _refreshToken;

    private readonly HttpClient _http;
    private readonly IConnectivityService _connectivity;

    public ApiClient(HttpClient http, IConnectivityService connectivity)
    {
        _http = http;
        _connectivity = connectivity;
    }

    // Token helpers

    private static async Task EnsureTokensLoadedAsync()
    {
        if ( _tokensLoaded )
            return;

        await TokenLock.WaitAsync();
        try
        {
            if ( _tokensLoaded )
                return;

            try
            {
                _accessToken = await SecureStorage.Default.GetAsync( AccessTokenKey );
                _refreshToken = await SecureStorage.Default.GetAsync( RefreshTokenKey );
            }
            catch
            {
                _accessToken = null;
                _refreshToken = null;
            }

            _tokensLoaded = true;
        }
        finally
        {
            TokenLock.Release();
        }
    }

    public static async Task SaveTokensAsync( AccessTokenResponse tokens )
    {
        await EnsureTokensLoadedAsync();

        _accessToken = tokens.AccessToken;
        _refreshToken = tokens.RefreshToken;

        try
        {
            await SecureStorage.Default.SetAsync( AccessTokenKey, tokens.AccessToken );
            await SecureStorage.Default.SetAsync( RefreshTokenKey, tokens.RefreshToken );
        }
        catch
        {
            // Keep the in-memory session alive if secure storage is unavailable.
        }
    }

    public static async Task<bool> HasStoredTokensInStorageAsync()
    {
        await EnsureTokensLoadedAsync();
        return _accessToken is not null;
    }

    public static async Task ClearStoredTokensAsync()
    {
        await EnsureTokensLoadedAsync();

        _accessToken = null;
        _refreshToken = null;

        try
        {
            SecureStorage.Default.Remove( AccessTokenKey );
            SecureStorage.Default.Remove( RefreshTokenKey );
        }
        catch
        {
            // Ignore storage cleanup errors.
        }
    }

    Task<bool> IApiClient.HasStoredTokensAsync() => HasStoredTokensInStorageAsync();

    Task IApiClient.ClearTokensAsync() => ClearStoredTokensAsync();

    // Request helpers

    private void AttachBearer( HttpRequestMessage request )
    {
        if ( _accessToken is { } token )
            request.Headers.Authorization = new AuthenticationHeaderValue( "Bearer", token );
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync( HttpRequestMessage request,
        CancellationToken ct = default )
    {
        await EnsureTokensLoadedAsync();
        AttachBearer( request );

        var retry = await CloneRequestAsync(request, ct);
        var response = await SendCoreAsync(request, ct);

        if ( response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _refreshToken is not null )
        {
            var refreshed = await RefreshTokensAsync( ct );
            if ( refreshed )
            {
                AttachBearer( retry );
                response.Dispose();
                response = await SendCoreAsync(retry, ct);
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original, CancellationToken ct)
    {
        var clone = new HttpRequestMessage( original.Method, original.RequestUri );

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (original.Content is not null)
        {
            var body = await original.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(body);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!_connectivity.IsOnline)
            throw new NetworkUnavailableException();

        try
        {
            return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
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

    private async Task<bool> RefreshTokensAsync( CancellationToken ct )
    {
        try
        {
            await EnsureTokensLoadedAsync();

            if ( _refreshToken is null )
                return false;

            var response = await SendCoreAsync(
                JsonRequest(
                    HttpMethod.Post,
                    "/api/auth/refresh",
                    new RefreshRequest { RefreshToken = _refreshToken }),
                ct);
            if ( !response.IsSuccessStatusCode )
            {
                await ClearStoredTokensAsync();
                return false;
            }

            var tokens = await DeserializeAsync<AccessTokenResponse>( response, ct );
            if ( tokens is null )
            {
                await ClearStoredTokensAsync();
                return false;
            }

            await SaveTokensAsync( tokens );
            return true;
        }
        catch
        {
            await ClearStoredTokensAsync();
            return false;
        }
    }

    private static async Task<T?> DeserializeAsync<T>( HttpResponseMessage response, CancellationToken ct )
    {
        var json = await response.Content.ReadAsStringAsync( ct );
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static HttpRequestMessage JsonRequest(HttpMethod method, string path, object body) =>
        new(method, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

    private async Task<T?> SendForResultAsync<T>(HttpMethod method, string path, CancellationToken ct)
    {
        var response = await SendWithRefreshAsync(new HttpRequestMessage(method, path), ct);
        return response.IsSuccessStatusCode ? await DeserializeAsync<T>(response, ct) : default;
    }

    private Task<T?> GetAsync<T>(string path, CancellationToken ct) =>
        SendForResultAsync<T>(HttpMethod.Get, path, ct);

    private async Task<List<T>> GetListAsync<T>(string path, CancellationToken ct) =>
        await GetAsync<List<T>>(path, ct) ?? [];

    private async Task<bool> SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        var response = await SendWithRefreshAsync(new HttpRequestMessage(method, path), ct);
        return response.IsSuccessStatusCode;
    }

    private static async Task<string> AuthErrorAsync(
        HttpResponseMessage response,
        AuthOperation operation,
        CancellationToken ct) =>
        AuthErrorMessages.FromResponse(
            response.StatusCode,
            await response.Content.ReadAsStringAsync(ct),
            operation);

    // Public API methods

    /// <summary>POST /api/auth/register</summary>
    public async Task<(bool Success, string? Error)> RegisterAsync( string email, string password,
        CancellationToken ct = default )
    {
        var response = await SendCoreAsync(
            JsonRequest(HttpMethod.Post, "/api/auth/register", new RegisterRequest
            {
                Email = email,
                Password = password
            }),
            ct);
        if ( response.IsSuccessStatusCode )
            return (true, null);

        return (false, await AuthErrorAsync(response, AuthOperation.Register, ct));
    }

    /// <summary>POST /api/auth/login</summary>
    public async Task<(bool Success, string? Error)> LoginAsync( string email, string password,
        CancellationToken ct = default )
    {
        var response = await SendCoreAsync(
            JsonRequest(HttpMethod.Post, "/api/auth/login", new LoginRequest
            {
                Email = email,
                Password = password
            }),
            ct);
        if ( !response.IsSuccessStatusCode )
        {
            return (false, await AuthErrorAsync(response, AuthOperation.Login, ct));
        }

        var tokens = await DeserializeAsync<AccessTokenResponse>( response, ct );
        if ( tokens is null )
            return (false, "Invalid response from server.");

        await SaveTokensAsync( tokens );
        return (true, null);
    }

    /// <summary>POST /api/auth/manage/info</summary>
    public async Task<(bool Success, string? Error)> ChangePasswordAsync( string oldPassword, string newPassword,
        CancellationToken ct = default )
    {
        var response = await SendWithRefreshAsync(
            JsonRequest(HttpMethod.Post, "/api/auth/manage/info", new ChangePasswordRequest
            {
                OldPassword = oldPassword,
                NewPassword = newPassword
            }),
            ct);
        if ( response.IsSuccessStatusCode )
            return (true, null);

        return (false, await AuthErrorAsync(response, AuthOperation.ChangePassword, ct));
    }

    /// <summary>GET /api/auth/manage/info</summary>
    public Task<InfoResponse?> GetProfileAsync(CancellationToken ct = default) =>
        GetAsync<InfoResponse>("/api/auth/manage/info", ct);

    /// <summary>GET /api/links/get?page=&amp;limit=&amp;categoryId=</summary>
    public async Task<PaginatedLinksResponse> GetLinksPageAsync( int page = 1, int limit = 20, int? categoryId = null,
        CancellationToken ct = default )
    {
        return await GetAsync<PaginatedLinksResponse>(
                   UriQueryBuilder.Build(
                       "/api/links/get",
                       ("page", page),
                       ("limit", limit),
                       ("categoryId", categoryId)),
                   ct)
               ?? new PaginatedLinksResponse();
    }

    public async Task<List<LinkModel>> GetLinksAsync( int page = 1, int limit = 20, int? categoryId = null,
        CancellationToken ct = default )
    {
        return (await GetLinksPageAsync(page, limit, categoryId, ct)).Items;
    }

    /// <summary>POST /api/links/shorten?originalUrl=&amp;expiresAt=&amp;isPublic=</summary>
    public async Task<LinkCreationResult> ShortenLinkAsync( string originalUrl, DateTime? expiresAt = null,
        bool isPublic = true, int? categoryId = null, CancellationToken ct = default )
    {
        var query = UriQueryBuilder.Build(
            "/api/links/shorten",
            ("originalUrl", originalUrl),
            ("isPublic", isPublic),
            ("expiresAt", expiresAt),
            ("categoryId", categoryId));

        var request = new HttpRequestMessage( HttpMethod.Post, query );
        var response = await SendWithRefreshAsync( request, ct );
        if ( response.IsSuccessStatusCode )
        {
            var created = await DeserializeAsync<CreatedLinkResponse>(response, ct);
            return new LinkCreationResult
            {
                Success = true,
                ShortUrl = created?.ShortUrl
            };
        }

        var error = await response.Content.ReadAsStringAsync( ct );
        return new LinkCreationResult
        {
            Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {error}"
        };
    }

    /// <summary>POST /api/links/remove?linkId=</summary>
    public Task<bool> RemoveLinkAsync(int linkId, CancellationToken ct = default) =>
        SendAsync(
            HttpMethod.Post,
            UriQueryBuilder.Build("/api/links/remove", ("linkId", linkId)),
            ct);

    /// <summary>GET /api/links/{id}/stats</summary>
    public Task<LinkStatsModel?> GetLinkStatsAsync(int id, CancellationToken ct = default) =>
        GetAsync<LinkStatsModel>($"/api/links/{id}/stats", ct);

    /// <summary>GET /api/links/{id}/activity</summary>
    public Task<List<LinkActivityModel>> GetLinkActivityAsync(int id, CancellationToken ct = default) =>
        GetListAsync<LinkActivityModel>($"/api/links/{id}/activity", ct);

    /// <summary>GET /api/links/favourites</summary>
    public Task<List<LinkModel>> GetFavouritesAsync(CancellationToken ct = default) =>
        GetListAsync<LinkModel>("/api/links/favourites", ct);

    /// <summary>POST /api/links/favourite?linkId=</summary>
    public Task<bool> AddFavouriteAsync(int linkId, CancellationToken ct = default) =>
        SendAsync(
            HttpMethod.Post,
            UriQueryBuilder.Build("/api/links/favourite", ("linkId", linkId)),
            ct);

    /// <summary>POST /api/links/unfavourite?linkId=</summary>
    public Task<bool> RemoveFavouriteAsync(int linkId, CancellationToken ct = default) =>
        SendAsync(
            HttpMethod.Post,
            UriQueryBuilder.Build("/api/links/unfavourite", ("linkId", linkId)),
            ct);

    /// <summary>GET /api/links/categories</summary>
    public Task<List<CategoryModel>> GetCategoriesAsync(CancellationToken ct = default) =>
        GetListAsync<CategoryModel>("/api/links/categories", ct);

    /// <summary>POST /api/links/category?categoryName=</summary>
    public Task<CategoryModel?> CreateCategoryAsync(string categoryName, CancellationToken ct = default) =>
        SendForResultAsync<CategoryModel>(
            HttpMethod.Post,
            UriQueryBuilder.Build("/api/links/category", ("categoryName", categoryName)),
            ct);

    /// <summary>POST /api/links/category?categoryName=</summary>
    public async Task<bool> AddCategoryAsync( string categoryName, CancellationToken ct = default )
    {
        return await CreateCategoryAsync(categoryName, ct) is not null;
    }

    /// <summary>DELETE /api/links/category/{id}</summary>
    public Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/api/links/category/{id}", ct);

    /// <summary>PUT /api/links/{id}/category?categoryId=</summary>
    public Task<bool> AssignCategoryAsync(int linkId, int? categoryId, CancellationToken ct = default) =>
        SendAsync(
            HttpMethod.Put,
            UriQueryBuilder.Build($"/api/links/{linkId}/category", ("categoryId", categoryId)),
            ct);
}
