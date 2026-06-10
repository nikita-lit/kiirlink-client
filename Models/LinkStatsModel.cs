using System.Text.Json.Serialization;

namespace KiirLink.Models;

public class LinkStatsModel
{
    [JsonPropertyName( "totalClicks" )] public int Clicks { get; set; }

    [JsonPropertyName( "favouritesCount" )]
    public int Favourites { get; set; }

    [JsonPropertyName( "byDate" )] public List<DailyStatModel> DailyViews { get; set; } = [];

    [JsonPropertyName( "byCountry" )] public List<CountryStatModel> Countries { get; set; } = [];

    // Kept for compatibility with older API responses. The analytics UI no longer displays sources.
    [JsonPropertyName( "bySource" )] public List<TrafficSourceModel> Sources { get; set; } = [];
}

public class DailyStatModel
{
    // server returns string date "yyyy-MM-dd"
    [JsonPropertyName( "date" )] public string DateStr { get; set; } = string.Empty;

    [JsonIgnore] public DateTime Date => DateTime.TryParse( DateStr, out var d ) ? d : DateTime.UtcNow;

    [JsonPropertyName( "count" )] public int Count { get; set; }
}

public class TrafficSourceModel
{
    [JsonPropertyName( "source" )] public string Source { get; set; } = string.Empty;

    [JsonPropertyName( "count" )] public int Count { get; set; }

    [JsonIgnore] public float Percentage { get; set; } // We will calculate this manually
}

public class CountryStatModel
{
    [JsonPropertyName( "country" )] public string Country { get; set; } = string.Empty;

    [JsonPropertyName( "count" )] public int Count { get; set; }

    [JsonIgnore] public float Percentage { get; set; }
}

public class LinkActivityModel
{
    [JsonPropertyName( "type" )] public string Type { get; set; } = string.Empty;

    [JsonPropertyName( "description" )] public string Description { get; set; } = string.Empty;

    [JsonPropertyName( "timestamp" )] public DateTime Timestamp { get; set; }

    [JsonIgnore]
    public string RelativeTime
    {
        get
        {
            var diff = DateTime.UtcNow - Timestamp.ToUniversalTime();
            if ( diff.TotalMinutes < 60 ) return $"{(int)diff.TotalMinutes} min ago";
            if ( diff.TotalHours < 24 ) return $"{(int)diff.TotalHours} h ago";
            return Timestamp.ToString( "MMM d, yyyy" );
        }
    }
}
