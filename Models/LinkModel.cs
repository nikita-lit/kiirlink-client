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

    // The API uses "clickCount" instead of "views"
    [JsonPropertyName( "clickCount" )] public int Views { get; set; }

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
    public string DisplayViews => Views.ToString();
}