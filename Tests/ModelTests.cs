using KiirLink.Models;
using System.Text.Json;

namespace KiirLink.Tests;

public sealed class ModelTests
{
    [Fact]
    public void LinkModel_UsesFavouriteLinkIdAndFormatsDisplayValues()
    {
        var model = new LinkModel
        {
            Id = 1,
            LinkId = 42,
            ShortUrl = "docs",
            Clicks = 12,
            CreatedAt = new DateTime(2026, 6, 10)
        };

        Assert.Equal(42, model.ResolvedId);
        Assert.Equal("kiirlink.ee/docs", model.DisplayTitle);
        Assert.Equal("12", model.DisplayClicks);
        Assert.Contains("2026", model.DisplayDate);
    }

    [Fact]
    public void DailyStatModel_ParsesApiDate()
    {
        var model = new DailyStatModel { DateStr = "2026-06-10" };

        Assert.Equal(new DateTime(2026, 6, 10), model.Date.Date);
    }

    [Fact]
    public void LinkStatsModel_DeserializesCountryBreakdown()
    {
        const string json = """
            {
              "byCountry": [
                { "country": "Estonia", "count": 3 }
              ]
            }
            """;

        var model = JsonSerializer.Deserialize<LinkStatsModel>(json);

        var country = Assert.Single(model!.Countries);
        Assert.Equal("Estonia", country.Country);
        Assert.Equal(3, country.Count);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("2026-06-10T11:00:00Z", "Expired")]
    [InlineData("2026-06-10T17:00:00Z", "Expires in 5h")]
    [InlineData("2026-06-13T12:00:00Z", "Expires in 3d")]
    public void LinkModel_FormatsExpiration(string? expiration, string expected)
    {
        var expiresAt = expiration is null ? (DateTime?)null : DateTime.Parse(expiration).ToUniversalTime();
        var now = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(expected, LinkModel.FormatExpiration(expiresAt, now));
    }

    [Fact]
    public void LinkExpiration_UsesEndOfSelectedLocalDay()
    {
        var selectedDate = new DateTime(2026, 7, 10);

        var expiration = LinkExpiration.EndOfSelectedDayUtc(selectedDate);

        Assert.Equal(
            selectedDate.AddDays(1).AddTicks(-1),
            expiration!.Value.ToLocalTime());
    }
}
