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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    // ── Token helpers ──────────────────────────────────────────────────────────

    private static string? StoredAccessToken
    {
        get => Preferences.Get("access_token", null);
        set
        {
            if (value is null) Preferences.Remove("access_token");
            else Preferences.Set("access_token", value);
        }
    }

    private static string? StoredRefreshToken
    {
        get => Preferences.Get("refresh_token", null);
        set
        {
            if (value is null) Preferences.Remove("refresh_token");
            else Preferences.Set("refresh_token", value);
        }
    }

    public static bool IsAuthenticated => StoredAccessToken is not null;

    public static void SaveTokens(AccessTokenResponse tokens)
    {
        StoredAccessToken = tokens.AccessToken;
        StoredRefreshToken = tokens.RefreshToken;
    }

    public static void ClearTokens()
    {
        StoredAccessToken = null;
        StoredRefreshToken = null;
    }

    // ── Request helpers ────────────────────────────────────────────────────────

    private void AttachBearer(HttpRequestMessage request)
    {
        if (StoredAccessToken is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        AttachBearer(request);
        var response = await _http.SendAsync(request, ct);

        // 401 → try refresh once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && StoredRefreshToken is not null)
        {
            var refreshed = await RefreshTokensAsync(ct);
            if (refreshed)
            {
                // Re-create request (cannot reuse disposed message)
                var retry = CloneRequest(request);
                AttachBearer(retry);
                response = await _http.SendAsync(retry, ct);
            }
        }

        return response;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        if (original.Content is StringContent sc)
        {
            var body = sc.ReadAsStringAsync().GetAwaiter().GetResult();
            clone.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }
        return clone;
    }

    private async Task<bool> RefreshTokensAsync(CancellationToken ct)
    {
        try
        {
            var body = JsonSerializer.Serialize(new RefreshRequest { RefreshToken = StoredRefreshToken! });
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) { ClearTokens(); return false; }

            var tokens = await DeserializeAsync<AccessTokenResponse>(response, ct);
            if (tokens is null) { ClearTokens(); return false; }

            SaveTokens(tokens);
            return true;
        }
        catch
        {
            ClearTokens();
            return false;
        }
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    // ── Public API methods ─────────────────────────────────────────────────────

    /// <summary>POST /api/auth/register</summary>
    public async Task<(bool Success, string? Error)> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new RegisterRequest { Email = email, Password = password });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        var error = await response.Content.ReadAsStringAsync(ct);
        return (false, error);
    }

    /// <summary>POST /api/auth/login</summary>
    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new LoginRequest { Email = email, Password = password });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            return (false, error);
        }
        var tokens = await DeserializeAsync<AccessTokenResponse>(response, ct);
        if (tokens is null) return (false, "Invalid response from server.");
        SaveTokens(tokens);
        return (true, null);
    }

    /// <summary>GET /api/auth/manage/info</summary>
    public async Task<InfoResponse?> GetProfileAsync(CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/manage/info");
        var response = await SendWithRefreshAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await DeserializeAsync<InfoResponse>(response, ct);
    }

    private class PaginatedLinksResponse
    {
        public List<LinkModel> Items { get; set; } = [];
        public int TotalCount { get; set; }
    }

    /// <summary>GET /api/links/get?page=&amp;limit=&amp;categoryId=</summary>
    public async Task<List<LinkModel>> GetLinksAsync(int page = 1, int limit = 20, int? categoryId = null, CancellationToken ct = default)
    {
        var query = $"/api/links/get?page={page}&limit={limit}";
        if (categoryId.HasValue) query += $"&categoryId={categoryId}";

        var request = new HttpRequestMessage(HttpMethod.Get, query);
        var response = await SendWithRefreshAsync(request, ct);
        if (!response.IsSuccessStatusCode) return [];
        
        var data = await DeserializeAsync<PaginatedLinksResponse>(response, ct);
        return data?.Items ?? [];
    }

    /// <summary>POST /api/links/shorten?originalUrl=&amp;expiresAt=&amp;isPublic=</summary>
    public async Task<(bool Success, string? Error)> ShortenLinkAsync(string originalUrl, DateTime? expiresAt = null, bool isPublic = true, CancellationToken ct = default)
    {
        var query = $"/api/links/shorten?originalUrl={Uri.EscapeDataString(originalUrl)}&isPublic={isPublic}";
        if (expiresAt.HasValue) query += $"&expiresAt={Uri.EscapeDataString(expiresAt.Value.ToString("o"))}";

        var request = new HttpRequestMessage(HttpMethod.Post, query);
        var response = await SendWithRefreshAsync(request, ct);
        if (response.IsSuccessStatusCode)
            return (true, null);

        var error = await response.Content.ReadAsStringAsync(ct);
        return (false, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {error}");
    }

    /// <summary>POST /api/links/remove?linkId=</summary>
    public async Task<bool> RemoveLinkAsync(int linkId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/links/remove?linkId={linkId}");
        var response = await SendWithRefreshAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>GET /api/links/{id}/stats</summary>
    public async Task<LinkStatsModel?> GetLinkStatsAsync(int id, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{id}/stats");
        var response = await SendWithRefreshAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await DeserializeAsync<LinkStatsModel>(response, ct);
    }

    /// <summary>GET /api/links/{id}/activity</summary>
    public async Task<List<LinkActivityModel>> GetLinkActivityAsync(int id, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/links/{id}/activity");
        var response = await SendWithRefreshAsync(request, ct);
        if (!response.IsSuccessStatusCode) return [];
        return await DeserializeAsync<List<LinkActivityModel>>(response, ct) ?? [];
    }

    /// <summary>GET /api/links/favourites</summary>
    public async Task<List<LinkModel>> GetFavouritesAsync(CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/links/favourites");
        var response = await SendWithRefreshAsync(request, ct);
        if (!response.IsSuccessStatusCode) return [];
        return await DeserializeAsync<List<LinkModel>>(response, ct) ?? [];
    }

    /// <summary>POST /api/links/favourite?linkId=</summary>
    public async Task<bool> AddFavouriteAsync(int linkId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/links/favourite?linkId={linkId}");
        var response = await SendWithRefreshAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>POST /api/links/unfavourite?linkId=</summary>
    public async Task<bool> RemoveFavouriteAsync(int linkId, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/links/unfavourite?linkId={linkId}");
        var response = await SendWithRefreshAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>GET /api/links/categories</summary>
    public async Task<List<CategoryModel>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/links/categories");
        var response = await SendWithRefreshAsync(request, ct);
        if (!response.IsSuccessStatusCode) return [];
        return await DeserializeAsync<List<CategoryModel>>(response, ct) ?? [];
    }

    /// <summary>POST /api/links/category?categoryName=</summary>
    public async Task<bool> AddCategoryAsync(string categoryName, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/links/category?categoryName={Uri.EscapeDataString(categoryName)}");
        var response = await SendWithRefreshAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>DELETE /api/links/category/{id}</summary>
    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/links/category/{id}");
        var response = await SendWithRefreshAsync(request, ct);
        return response.IsSuccessStatusCode;
    }

    /// <summary>PUT /api/links/{id}/category?categoryId=</summary>
    public async Task<bool> AssignCategoryAsync(int linkId, int? categoryId, CancellationToken ct = default)
    {
        var query = $"/api/links/{linkId}/category";
        if (categoryId.HasValue) query += $"?categoryId={categoryId}";
        var request = new HttpRequestMessage(HttpMethod.Put, query);
        var response = await SendWithRefreshAsync(request, ct);
        return response.IsSuccessStatusCode;
    }
}
