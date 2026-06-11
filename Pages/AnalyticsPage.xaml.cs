using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Graphics;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Pages;

public partial class AnalyticsPage
{
    private readonly LinkService _linkService;
    private readonly PerformanceChartDrawable _chartDrawable = new();

    // The link whose analytics are shown; updated from LinksPage when a card is tapped.
    public static int SelectedLinkId { get; set; } = -1;
    public static LinkModel? SelectedLink { get; set; }

    public AnalyticsPage( LinkService linkService )
    {
        InitializeComponent();
        _linkService = linkService;
        PerformanceChart.Drawable = _chartDrawable;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAnalyticsAsync();
    }

    // ── Data loading ─────────────────────────────────────────────────────────

    private async Task LoadAnalyticsAsync()
    {
        ResetAnalyticsState();

        if ( SelectedLink is not null )
            PopulateTopCard( SelectedLink );

        if ( SelectedLinkId <= 0 )
        {
            if ( SelectedLink is not null )
                SelectedLinkId = SelectedLink.ResolvedId;
            else
            {
                try
                {
                    var links = await _linkService.GetLinksAsync( 1, 1 );
                    if ( links.Count > 0 )
                    {
                        SelectedLinkId = links[0].Id;
                        SelectedLink = links[0];
                        PopulateTopCard( links[0] );
                    }
                    else
                    {
                        ShowEmpty();
                        return;
                    }
                }
                catch
                {
                    ShowEmpty();
                    return;
                }
            }
        }

        if ( await LoadStatsAsync() )
            await LoadActivityAsync();
        else
            BuildActivityLayout( [] );
    }

    private async Task<bool> LoadStatsAsync()
    {
        try
        {
            var stats = await _linkService.GetLinkStatsAsync( SelectedLinkId );

            if ( stats is null || !AnalyticsDataState.HasStatistics( stats ) )
            {
                if ( SelectedLink is null )
                {
                    var links = await _linkService.GetLinksAsync( 1, 20 );
                    var link = links.FirstOrDefault( l => l.ResolvedId == SelectedLinkId )
                               ?? links.FirstOrDefault();

                    if ( link is not null )
                    {
                        SelectedLink = link;
                        PopulateTopCard( link );
                    }
                }

                ShowNoStatistics();
                return false;
            }

            NoStatisticsBanner.IsVisible = false;
            ClicksLabel.Text = stats.Clicks.ToString();
            //FavouritesLabel.Text = stats.Favourites.ToString();

            // Chart
            var dailyViews = stats.DailyViews ?? [];
            if ( dailyViews.Count > 0 )
            {
                _chartDrawable.SetData( dailyViews );
                PerformanceChart.IsVisible = true;
                PerformanceEmptyLabel.IsVisible = false;
                PerformanceChart.Invalidate();
            }
            else
            {
                ShowEmptyPerformance();
            }

            BuildCountriesLayout( stats.Countries ?? [] );
            return true;
        }
        catch ( Exception ex )
        {
            ShowNoStatistics();
            await DisplayAlertAsync( L("Error"), F("CouldNotLoadStats", ex.Message), "OK" );
            return false;
        }
    }

    private async Task LoadActivityAsync()
    {
        try
        {
            var activity = await _linkService.GetLinkActivityAsync( SelectedLinkId );
            BuildActivityLayout( activity );
        }
        catch
        {
            // non-critical
        }
    }

    private void PopulateTopCard( LinkModel link )
    {
        TopLinkCard.Title = link.DisplayTitle;
        TopLinkCard.Url = link.OriginalUrl;
        TopLinkCard.Clicks = link.Clicks;
        TopLinkCard.Category = link.CategoryName ?? string.Empty;
        TopLinkCard.Date = link.DisplayDate;
        TopLinkCard.ExpiresAt = link.ExpiresAt;
        TopLinkCard.IsPublic = link.IsPublic;
        TopLinkCard.LinkId = link.ResolvedId;
        TopLinkCard.ShortUrl = link.ShortUrl;
        TopLinkCard.IsFavourite = link.IsFavourite;
    }

    private void ShowEmpty()
    {
        ShowNoStatistics( "No link is available to show statistics for." );
        BuildActivityLayout( [] );
    }

    private void ResetAnalyticsState()
    {
        NoStatisticsBanner.IsVisible = false;
        ClicksLabel.Text = "—";
        //FavouritesLabel.Text = "—";
        _chartDrawable.Clear();
        PerformanceChart.Invalidate();
        ShowEmptyPerformance();
        BuildCountriesLayout( [] );
        BuildActivityLayout( [] );
    }

    private void ShowNoStatistics( string message = "No statistics are available for this link yet." )
    {
        ClicksLabel.Text = "—";
        //FavouritesLabel.Text = "—";
        NoStatisticsLabel.Text = message;
        NoStatisticsBanner.IsVisible = true;
        _chartDrawable.Clear();
        PerformanceChart.Invalidate();
        ShowEmptyPerformance();
        BuildCountriesLayout( [] );
    }

    private void ShowEmptyPerformance()
    {
        PerformanceChart.IsVisible = false;
        PerformanceEmptyLabel.IsVisible = true;
    }

    // ── Dynamic layout builders ───────────────────────────────────────────────

    private void BuildCountriesLayout( List<CountryStatModel> countries )
    {
        CountriesLayout.Children.Clear();

        if ( countries.Count == 0 )
        {
            CountriesLayout.Children.Add( new Label
            {
                Text = "No country data yet.",
                FontSize = 11,
                Margin = new Thickness( 0, 4 ),
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = GetThemeColor( "AppMutedText" )
            } );
            return;
        }

        var total = countries.Sum( country => country.Count );
        foreach ( var country in countries.OrderByDescending( country => country.Count ) )
        {
            country.Percentage = total > 0 ? (float)country.Count / total : 0;
        }

        foreach ( var country in countries )
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = new GridLength( 100 ) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength( 38 ) }
                ]
            };

            var countryName = string.IsNullOrWhiteSpace( country.Country )
                ? "Unknown"
                : country.Country;
            var nameLabel = new Label
                { FontSize = 10, Text = countryName, VerticalTextAlignment = TextAlignment.Center };
            var bar = new ProgressBar { Progress = country.Percentage, VerticalOptions = LayoutOptions.Center };
            var pctLabel = new Label
            {
                FontSize = 10,
                Text = $"{(int)(country.Percentage * 100)}%",
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn( nameLabel, 0 );
            Grid.SetColumn( bar, 1 );
            Grid.SetColumn( pctLabel, 2 );

            grid.Children.Add( nameLabel );
            grid.Children.Add( bar );
            grid.Children.Add( pctLabel );

            CountriesLayout.Children.Add( grid );
        }
    }

    private void BuildActivityLayout( List<LinkActivityModel> activities )
    {
        ActivityLayout.Children.Clear();

            if ( activities.Count == 0 )
            {
                ActivityLayout.Children.Add( new Label
                {
                    Text = "No recent activity.",
                    FontSize = 11,
                    Margin = new Thickness( 0, 12 ),
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = GetThemeColor( "AppMutedText" )
                } );
                return;
            }

        for ( var i = 0; i < activities.Count; i++ )
        {
            var act = activities[i];
            var grid = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = new GridLength( 26 ) },
                    new ColumnDefinition { Width = GridLength.Star }
                ],
                RowDefinitions =
                [
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                ],
                Padding = new Thickness( 0, 8 ),
                RowSpacing = 2
            };

            var icon = new Image
            {
                HeightRequest = 18,
                WidthRequest = 18,
                Source = act.Type == "click" ? "click.svg" : "clock.svg",
                VerticalOptions = LayoutOptions.Start
            };
            icon.Behaviors.Add( new IconTintColorBehavior
            {
                TintColor = GetThemeColor( "AppAccentText" )
            } );
            var desc = new Label
            {
                FontSize = 11,
                LineBreakMode = LineBreakMode.WordWrap,
                Text = act.Description,
                VerticalTextAlignment = TextAlignment.Start
            };
            var time = new Label
            {
                FontSize = 10,
                Text = act.RelativeTime,
                TextColor = GetThemeColor( "AppMutedText" ),
                VerticalTextAlignment = TextAlignment.Start
            };

            Grid.SetColumn( icon, 0 );
            Grid.SetRowSpan( icon, 2 );
            Grid.SetColumn( desc, 1 );
            Grid.SetColumn( time, 1 );
            Grid.SetRow( time, 1 );

            grid.Children.Add( icon );
            grid.Children.Add( desc );
            grid.Children.Add( time );

            ActivityLayout.Children.Add( grid );

            if ( i < activities.Count - 1 )
                ActivityLayout.Children.Add( new BoxView
                {
                    BackgroundColor = GetThemeColor( "AppDivider" ),
                    HeightRequest = 1
                } );
        }
    }

    // ── Chart drawable ────────────────────────────────────────────────────────

    private sealed class PerformanceChartDrawable : IDrawable
    {
        private float[] _values = [];
        private string[] _labels = [];

        public void SetData( List<DailyStatModel> daily )
        {
            // Take last 7 days
            var slice = daily.TakeLast( 7 ).ToList();
            _values = slice.Select( d => (float)d.Count ).ToArray();
            _labels = slice.Select( d => d.Date.ToString( "ddd" ) ).ToArray();
        }

        public void Clear()
        {
            _values = [];
            _labels = [];
        }

        public void Draw( ICanvas canvas, RectF dirtyRect )
        {
            if ( _values.Length == 0 ) return;

            const float left = 28;
            const float top = 8;
            const float bottom = 22;
            const float right = 8;

            var chartWidth = dirtyRect.Width - left - right;
            var chartHeight = dirtyRect.Height - top - bottom;
            var maxValue = _values.Max();
            if ( maxValue == 0 ) maxValue = 1;

            canvas.FontColor = GetThemeColor( "AppMutedText" );
            canvas.FontSize = 8;
            canvas.StrokeColor = GetThemeColor( "AppDivider" );
            canvas.StrokeSize = 1;

            for ( var step = 0; step <= 4; step++ )
            {
                var y = top + chartHeight - (chartHeight * step / 4);
                canvas.DrawLine( left, y, dirtyRect.Width - right, y );
                canvas.DrawString( ((int)(maxValue * step / 4)).ToString(), 0, y - 5, left - 5, 10,
                    HorizontalAlignment.Right, VerticalAlignment.Center );
            }

            var points = new PointF[_values.Length];
            var spacing = _values.Length > 1 ? chartWidth / (_values.Length - 1) : 0;

            for ( var index = 0; index < _values.Length; index++ )
            {
                var x = left + (spacing * index);
                var y = top + chartHeight - (_values[index] / maxValue * chartHeight);
                points[index] = new PointF( x, y );
                canvas.DrawString( _labels[index], x - 12, dirtyRect.Height - 16, 24, 12,
                    HorizontalAlignment.Center, VerticalAlignment.Center );
            }

            canvas.StrokeColor = GetThemeColor( "AppAccentText" );
            canvas.StrokeSize = 2;

            for ( var index = 0; index < points.Length - 1; index++ )
                canvas.DrawLine( points[index], points[index + 1] );

            canvas.FillColor = GetThemeColor( "AppBackground" );

            foreach ( var point in points )
            {
                canvas.FillCircle( point, 3 );
                canvas.DrawCircle( point, 3 );
            }
        }
    }

    private static string L(string key) => LocalizationManager.Instance.Get(key);
    private static string F(string key, params object[] args) => LocalizationManager.Instance.Format(key, args);

    private static Color GetThemeColor( string key )
    {
        return (Color)Application.Current!.Resources[key];
    }
}
