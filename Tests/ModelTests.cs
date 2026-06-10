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
}
