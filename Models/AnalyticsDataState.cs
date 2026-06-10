namespace KiirLink.Models;

public static class AnalyticsDataState
{
    public static bool HasStatistics( LinkStatsModel? stats )
    {
        return stats is not null &&
               (stats.Clicks > 0 ||
                stats.Favourites > 0 ||
                stats.DailyViews?.Count > 0 ||
                stats.Countries?.Count > 0 ||
                stats.Sources?.Count > 0);
    }
}
