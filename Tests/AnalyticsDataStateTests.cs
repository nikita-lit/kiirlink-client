using KiirLink.Models;

namespace KiirLink.Tests;

public sealed class AnalyticsDataStateTests
{
    [Fact]
    public void HasStatistics_ReturnsFalse_WhenResponseIsMissing()
    {
        Assert.False( AnalyticsDataState.HasStatistics( null ) );
    }

    [Fact]
    public void HasStatistics_ReturnsFalse_WhenResponseContainsNoData()
    {
        Assert.False( AnalyticsDataState.HasStatistics( new LinkStatsModel() ) );
    }

    [Fact]
    public void HasStatistics_ReturnsFalse_WhenDetailedCollectionsAreNull()
    {
        var stats = new LinkStatsModel
        {
            DailyViews = null!,
            Sources = null!
        };

        Assert.False( AnalyticsDataState.HasStatistics( stats ) );
    }

    [Fact]
    public void HasStatistics_ReturnsTrue_WhenAggregateDataExists()
    {
        var stats = new LinkStatsModel { Clicks = 1 };

        Assert.True( AnalyticsDataState.HasStatistics( stats ) );
    }

    [Fact]
    public void HasStatistics_ReturnsTrue_WhenDetailedDataExists()
    {
        var stats = new LinkStatsModel
        {
            DailyViews = [new DailyStatModel { DateStr = "2026-06-10", Count = 0 }]
        };

        Assert.True( AnalyticsDataState.HasStatistics( stats ) );
    }

    [Fact]
    public void HasStatistics_ReturnsTrue_WhenCountryDataExists()
    {
        var stats = new LinkStatsModel
        {
            Countries = [new CountryStatModel { Country = "Estonia", Count = 1 }]
        };

        Assert.True( AnalyticsDataState.HasStatistics( stats ) );
    }
}
