using System.Text.Json.Serialization;

namespace KiirLink.Models;

public class LinkModel
{
    [JsonPropertyName( "id" )] public int Id { get; set; }

    // Favourites endpoint returns LinkId instead of Id for the link itself
    [JsonPropertyName( "linkId" )] public int? LinkId { get; set; }

    public int ResolvedId => LinkId ?? Id;

    [JsonPropertyName( "shortUrl" )] public string ShortUrl { get; set; } = string.Empty;

    [JsonPropertyName( "originalUrl" )] public string OriginalUrl { get; set; } = string.Empty;
    
    [JsonPropertyName( "clickCount" )] public int Clicks { get; set; }

    [JsonPropertyName( "favourites" )] public int Favourites { get; set; }

    [JsonPropertyName( "createdAt" )] public DateTime CreatedAt { get; set; }

    [JsonPropertyName( "expiresAt" )] public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName( "isPublic" )] public bool IsPublic { get; set; }

    [JsonPropertyName( "categoryId" )] public int? CategoryId { get; set; }

    // The API uses "category" instead of "categoryName"
    [JsonPropertyName( "category" )] public string? CategoryName { get; set; }

    [JsonPropertyName( "isFavourite" )] public bool IsFavourite { get; set; }

    public string DisplayTitle => $"kiirlink.ee/{ShortUrl}";
    public string DisplayDate => CreatedAt.ToString( "MMM d, yyyy" );
    public string DisplayClicks => Clicks.ToString();
    public string DisplayExpiration => FormatExpiration(ExpiresAt, DateTime.UtcNow);

    public static string FormatExpiration(DateTime? expiresAt, DateTime now)
    {
        if (!expiresAt.HasValue)
            return string.Empty;

        var remaining = expiresAt.Value.ToUniversalTime() - now.ToUniversalTime();
        if (remaining <= TimeSpan.Zero)
            return "Expired";

        if (remaining.TotalHours < 24)
            return $"Expires in {Math.Max(1, (int)Math.Ceiling(remaining.TotalHours))}h";

        if (remaining.TotalDays < 30)
            return $"Expires in {Math.Max(1, (int)Math.Ceiling(remaining.TotalDays))}d";

        return $"Expires {expiresAt.Value.ToLocalTime():MMM d, yyyy}";
    }
}

public sealed class LinkCreationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ShortUrl { get; init; }
}

public static class LinkExpiration
{
    public static DateTime? EndOfSelectedDayUtc(DateTime? selectedDate)
    {
        return selectedDate?.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
    }
}

public sealed class CreatedLinkResponse
{
    [JsonPropertyName("shortUrl")] public string ShortUrl { get; set; } = string.Empty;
}
