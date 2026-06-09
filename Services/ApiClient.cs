using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KiirLink.Models;

namespace KiirLink.Services;

/// <summary>
/// Low-level HTTP client for the KiirLink API.
/// Handles token injection, automatic refresh, and JSON serialization.
/// </summary>
public class ApiClient
{
    public const string BaseUrl = "http://88.196.25.201";

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

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri( BaseUrl ) };
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

    public static async Task<bool> HasStoredTokensAsync()
    {
        await EnsureTokensLoadedAsync();
        return _accessToken is not null;
    }

    public static async Task ClearTokensAsync()
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

        var response = await _http.SendAsync( request, ct );

        if ( response.StatusCode == System.Net.HttpStatusCode.Unauthorized && _refreshToken is not null )
        {
            var refreshed = await RefreshTokensAsync( ct );
            if ( refreshed )
            {
                var retry = CloneRequest( request );
                AttachBearer( retry );
                response = await _http.SendAsync( retry, ct );
            }
        }

        return response;
    }

    private static HttpRequestMessage CloneRequest( HttpRequestMessage original )
    {
        var clone = new HttpRequestMessage( original.Method, original.RequestUri );

        if ( original.Content is StringContent sc )
        {
            var body = sc.ReadAsStringAsync().GetAwaiter().GetResult();
            clone.Content = new StringContent( body, Encoding.UTF8, "application/json" );
        }

        return clone;
    }

    private async Task<bool> RefreshTokensAsync( CancellationToken ct )
    {
        try
        {
            await EnsureTokensLoadedAsync();

            if ( _refreshToken is null )
                return false;

            var body = JsonSerializer.Serialize( new RefreshRequest { RefreshToken = _refreshToken } );
            var request = new HttpRequestMessage( HttpMethod.Post, "/api/auth/refresh" )
            {
                Content = new StringContent( body, Encoding.UTF8, "application/json" )
            };

            var response = await _http.SendAsync( request, ct );
            if ( !response.IsSuccessStatusCode )
            {
                await ClearTokensAsync();
                return false;
            }

            var tokens = await DeserializeAsync<AccessTokenResponse>( response, ct );
            if ( tokens is null )
            {
                await ClearTokensAsync();
                return false;
            }

            await SaveTokensAsync( tokens );
            return true;
        }
        catch
        {
            await ClearTokensAsync();
            return false;
        }
    }

    private static async Task<T?> DeserializeAsync<T>( HttpResponseMessage response, CancellationToken ct )
    {
        var json = await response.Content.ReadAsStringAsync( ct );
        if ( string.IsNullOrWhiteSpace( json ) )
            return default;

        return JsonSerializer.Deserialize<T>( json, JsonOptions );
    }

    // Public API methods

    /// <summary>POST /api/auth/register</summary>
    public async Task<(bool Success, string? Error)> RegisterAsync( string email, string password,
        CancellationToken ct = default )
    {
        var body = JsonSerializer.Serialize( new RegisterRequest { Email = email, Password = password } );
        var request = new HttpRequestMessage( HttpMethod.Post, "/api/auth/register" )
        {
            Content = new StringContent( body, Encoding.UTF8, "application/json" )
        };

        var response = await _http.SendAsync( request, ct );
        if ( response.IsSuccessStatusCode )
            return (true, null);

        var error = await response.Content.ReadAsStringAsync( ct );
        return (false, error);
    }

    /// <summary>POST /api/auth/login</summary>
    public async Task<(bool Success, string? Error)> LoginAsync( string email, string password,
        CancellationToken ct = default )
    {
        var body = JsonSerializer.Serialize( new LoginRequest { Email = email, Password = password } );
        var request = new HttpRequestMessage( HttpMethod.Post, "/api/auth/login" )
        {
            Content = new StringContent( body, Encoding.UTF8, "application/json" )
        };

        var response = await _http.SendAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
        {
            var error = await response.Content.ReadAsStringAsync( ct );
            return (false, error);
        }

        var tokens = await DeserializeAsync<AccessTokenResponse>( response, ct );
        if ( tokens is null )
            return (false, "Invalid response from server.");

        await SaveTokensAsync( tokens );
        return (true, null);
    }

    /// <summary>GET /api/auth/manage/info</summary>
    public async Task<InfoResponse?> GetProfileAsync( CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Get, "/api/auth/manage/info" );
        var response = await SendWithRefreshAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
            return null;

        return await DeserializeAsync<InfoResponse>( response, ct );
    }

    public sealed class PaginatedLinksResponse
    {
        public List<LinkModel> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    /// <summary>GET /api/links/get?page=&amp;limit=&amp;categoryId=</summary>
    public async Task<PaginatedLinksResponse> GetLinksPageAsync( int page = 1, int limit = 20, int? categoryId = null,
        CancellationToken ct = default )
    {
        var query = $"/api/links/get?page={page}&limit={limit}";
        if ( categoryId.HasValue )
            query += $"&categoryId={categoryId}";

        var request = new HttpRequestMessage( HttpMethod.Get, query );
        var response = await SendWithRefreshAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
            return new PaginatedLinksResponse();

        var data = await DeserializeAsync<PaginatedLinksResponse>( response, ct );
        return data ?? new PaginatedLinksResponse();
    }

    public async Task<List<LinkModel>> GetLinksAsync( int page = 1, int limit = 20, int? categoryId = null,
        CancellationToken ct = default )
    {
        var pageData = await GetLinksPageAsync( page, limit, categoryId, ct );
        return pageData.Items;
    }

    /// <summary>POST /api/links/shorten?originalUrl=&amp;expiresAt=&amp;isPublic=</summary>
    public async Task<(bool Success, string? Error)> ShortenLinkAsync( string originalUrl, DateTime? expiresAt = null,
        bool isPublic = true, CancellationToken ct = default )
    {
        var query = $"/api/links/shorten?originalUrl={Uri.EscapeDataString( originalUrl )}&isPublic={isPublic}";
        if ( expiresAt.HasValue )
            query += $"&expiresAt={Uri.EscapeDataString( expiresAt.Value.ToString( "o" ) )}";

        var request = new HttpRequestMessage( HttpMethod.Post, query );
        var response = await SendWithRefreshAsync( request, ct );
        if ( response.IsSuccessStatusCode )
            return (true, null);

        var error = await response.Content.ReadAsStringAsync( ct );
        return (false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {error}");
    }

    /// <summary>POST /api/links/remove?linkId=</summary>
    public async Task<bool> RemoveLinkAsync( int linkId, CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Post, $"/api/links/remove?linkId={linkId}" );
        var response = await SendWithRefreshAsync( request, ct );
        return response.IsSuccessStatusCode;
    }

    /// <summary>GET /api/links/{id}/stats</summary>
    public async Task<LinkStatsModel?> GetLinkStatsAsync( int id, CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Get, $"/api/links/{id}/stats" );
        var response = await SendWithRefreshAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
            return null;

        return await DeserializeAsync<LinkStatsModel>( response, ct );
    }

    /// <summary>GET /api/links/{id}/activity</summary>
    public async Task<List<LinkActivityModel>> GetLinkActivityAsync( int id, CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Get, $"/api/links/{id}/activity" );
        var response = await SendWithRefreshAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
            return [];

        return await DeserializeAsync<List<LinkActivityModel>>( response, ct ) ?? [];
    }

    /// <summary>GET /api/links/favourites</summary>
    public async Task<List<LinkModel>> GetFavouritesAsync( CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Get, "/api/links/favourites" );
        var response = await SendWithRefreshAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
            return [];

        return await DeserializeAsync<List<LinkModel>>( response, ct ) ?? [];
    }

    /// <summary>POST /api/links/favourite?linkId=</summary>
    public async Task<bool> AddFavouriteAsync( int linkId, CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Post, $"/api/links/favourite?linkId={linkId}" );
        var response = await SendWithRefreshAsync( request, ct );
        return response.IsSuccessStatusCode;
    }

    /// <summary>POST /api/links/unfavourite?linkId=</summary>
    public async Task<bool> RemoveFavouriteAsync( int linkId, CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Post, $"/api/links/unfavourite?linkId={linkId}" );
        var response = await SendWithRefreshAsync( request, ct );
        return response.IsSuccessStatusCode;
    }

    /// <summary>GET /api/links/categories</summary>
    public async Task<List<CategoryModel>> GetCategoriesAsync( CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Get, "/api/links/categories" );
        var response = await SendWithRefreshAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
            return [];

        return await DeserializeAsync<List<CategoryModel>>( response, ct ) ?? [];
    }

    /// <summary>POST /api/links/category?categoryName=</summary>
    public async Task<CategoryModel?> CreateCategoryAsync( string categoryName, CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Post,
            $"/api/links/category?categoryName={Uri.EscapeDataString( categoryName )}" );
        var response = await SendWithRefreshAsync( request, ct );
        if ( !response.IsSuccessStatusCode )
            return null;

        return await DeserializeAsync<CategoryModel>( response, ct );
    }

    /// <summary>POST /api/links/category?categoryName=</summary>
    public async Task<bool> AddCategoryAsync( string categoryName, CancellationToken ct = default )
    {
        var category = await CreateCategoryAsync( categoryName, ct );
        return category is not null;
    }

    /// <summary>DELETE /api/links/category/{id}</summary>
    public async Task<bool> DeleteCategoryAsync( int id, CancellationToken ct = default )
    {
        var request = new HttpRequestMessage( HttpMethod.Delete, $"/api/links/category/{id}" );
        var response = await SendWithRefreshAsync( request, ct );
        return response.IsSuccessStatusCode;
    }

    /// <summary>PUT /api/links/{id}/category?categoryId=</summary>
    public async Task<bool> AssignCategoryAsync( int linkId, int? categoryId, CancellationToken ct = default )
    {
        var query = $"/api/links/{linkId}/category";
        if ( categoryId.HasValue )
            query += $"?categoryId={categoryId}";

        var request = new HttpRequestMessage( HttpMethod.Put, query );
        var response = await SendWithRefreshAsync( request, ct );
        return response.IsSuccessStatusCode;
    }
}