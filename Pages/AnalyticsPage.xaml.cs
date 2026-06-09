using Microsoft.Maui.Graphics;
using KiirLink.Models;
using KiirLink.Services;

namespace KiirLink.Pages;

public partial class AnalyticsPage : ContentPage
{
    private readonly LinkService _linkService;
    private PerformanceChartDrawable _chartDrawable = new();

    // The link whose analytics are shown; updated from LinksPage when a card is tapped.
    public static int SelectedLinkId { get; set; } = -1;
    public static LinkModel? SelectedLink { get; set; }

    public AnalyticsPage(LinkService linkService)
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
        if (SelectedLink is not null)
            PopulateTopCard(SelectedLink);

        if (SelectedLinkId <= 0)
        {
            if (SelectedLink is not null)
                SelectedLinkId = SelectedLink.ResolvedId;
            else
            {
                try
                {
                    var links = await _linkService.GetLinksAsync(1, 1);
                    if (links.Count > 0)
                    {
                        SelectedLinkId = links[0].Id;
                        SelectedLink = links[0];
                        PopulateTopCard(links[0]);
                    }
                    else { ShowEmpty(); return; }
                }
                catch { ShowEmpty(); return; }
            }
        }

        await Task.WhenAll(LoadStatsAsync(), LoadActivityAsync());
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            var stats = await _linkService.GetLinkStatsAsync(SelectedLinkId);

            if (stats is null)
            {
                if (SelectedLink is null)
                {
                    var links = await _linkService.GetLinksAsync(1, 20);
                    var link = links.FirstOrDefault(l => l.ResolvedId == SelectedLinkId)
                               ?? links.FirstOrDefault();

                    if (link is not null)
                    {
                        SelectedLink = link;
                        PopulateTopCard(link);
                    }
                }

                return;
            }

            ViewsLabel.Text = stats.Clicks.ToString(); // Server doesn't separate views/clicks
            ClicksLabel.Text = stats.Clicks.ToString();
            FavouritesLabel.Text = stats.Favourites.ToString();

            // Chart
            var dailyViews = stats.DailyViews ?? [];
            if (dailyViews.Count > 0)
            {
                _chartDrawable.SetData(dailyViews);
                PerformanceChart.Invalidate();
            }

            // Traffic sources
            BuildSourcesLayout(stats.Sources ?? []);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Could not load stats: {ex.Message}", "OK");
        }
    }

    private async Task LoadActivityAsync()
    {
        try
        {
            var activity = await _linkService.GetLinkActivityAsync(SelectedLinkId);
            BuildActivityLayout(activity);
        }
        catch
        {
            // non-critical
        }
    }

    private void PopulateTopCard(LinkModel link)
    {
        TopLinkCard.Title = link.DisplayTitle;
        TopLinkCard.Url = link.OriginalUrl;
        TopLinkCard.Views = link.DisplayViews;
        TopLinkCard.Category = link.CategoryName ?? string.Empty;
        TopLinkCard.Date = link.DisplayDate;

        ViewsLabel.Text = link.Views.ToString();
        ClicksLabel.Text = link.Views.ToString();
        FavouritesLabel.Text = link.Favourites.ToString();
    }

    private void ShowEmpty()
    {
        ViewsLabel.Text = "0";
        ClicksLabel.Text = "0";
        FavouritesLabel.Text = "0";
    }

    // ── Dynamic layout builders ───────────────────────────────────────────────

    private void BuildSourcesLayout(List<TrafficSourceModel> sources)
    {
        SourcesLayout.Children.Clear();

        if (sources.Count == 0)
        {
            // Fallback with static demo data
            sources =
            [
                new() { Source = "Google", Percentage = 0.45f },
                new() { Source = "Direct", Percentage = 0.35f },
                new() { Source = "Social", Percentage = 0.20f }
            ];
        }
        else
        {
            var total = sources.Sum(s => s.Count);
            foreach (var src in sources)
            {
                src.Percentage = total > 0 ? (float)src.Count / total : 0;
            }
        }

        foreach (var src in sources)
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = new GridLength(62) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength(38) }
                ]
            };

            var nameLabel = new Label { FontSize = 10, Text = src.Source, VerticalTextAlignment = TextAlignment.Center };
            var bar = new ProgressBar { Progress = src.Percentage, VerticalOptions = LayoutOptions.Center };
            var pctLabel = new Label
            {
                FontSize = 10,
                Text = $"{(int)(src.Percentage * 100)}%",
                HorizontalTextAlignment = TextAlignment.End,
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(nameLabel, 0);
            Grid.SetColumn(bar, 1);
            Grid.SetColumn(pctLabel, 2);

            grid.Children.Add(nameLabel);
            grid.Children.Add(bar);
            grid.Children.Add(pctLabel);

            SourcesLayout.Children.Add(grid);
        }
    }

    private void BuildActivityLayout(List<LinkActivityModel> activities)
    {
        ActivityLayout.Children.Clear();

        if (activities.Count == 0)
        {
            ActivityLayout.Children.Add(new Label
            {
                Text = "No recent activity.",
                FontSize = 11,
                Margin = new Thickness(0, 12),
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.FromArgb("#969696")
            });
            return;
        }

        for (var i = 0; i < activities.Count; i++)
        {
            var act = activities[i];
            var grid = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition { Width = new GridLength(26) },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                ],
                HeightRequest = 42
            };

            var icon = new Label
            {
                Text = act.Icon,
                TextColor = Color.FromArgb("#FF5A36"),
                VerticalTextAlignment = TextAlignment.Center
            };
            var desc = new Label
            {
                FontSize = 11,
                Text = act.Description,
                VerticalTextAlignment = TextAlignment.Center
            };
            var time = new Label
            {
                FontSize = 10,
                Text = act.RelativeTime,
                TextColor = Color.FromArgb("#969696"),
                VerticalTextAlignment = TextAlignment.Center
            };

            Grid.SetColumn(icon, 0);
            Grid.SetColumn(desc, 1);
            Grid.SetColumn(time, 2);

            grid.Children.Add(icon);
            grid.Children.Add(desc);
            grid.Children.Add(time);

            ActivityLayout.Children.Add(grid);

            if (i < activities.Count - 1)
                ActivityLayout.Children.Add(new BoxView
                {
                    BackgroundColor = Color.FromArgb("#F0F0F0"),
                    HeightRequest = 1
                });
        }
    }

    // ── Chart drawable ────────────────────────────────────────────────────────

    private sealed class PerformanceChartDrawable : IDrawable
    {
        private float[] _values = [20, 35, 56, 40, 80, 30, 58];
        private string[] _labels = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        public void SetData(List<DailyStatModel> daily)
        {
            // Take last 7 days
            var slice = daily.TakeLast(7).ToList();
            _values = slice.Select(d => (float)d.Count).ToArray();
            _labels = slice.Select(d => d.Date.ToString("ddd")).ToArray();
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            const float left = 28;
            const float top = 8;
            const float bottom = 22;
            const float right = 8;

            var chartWidth = dirtyRect.Width - left - right;
            var chartHeight = dirtyRect.Height - top - bottom;
            var maxValue = _values.Length > 0 ? _values.Max() : 80f;
            if (maxValue == 0) maxValue = 1;

            canvas.FontColor = Color.FromArgb("#969696");
            canvas.FontSize = 8;
            canvas.StrokeColor = Color.FromArgb("#ECECEC");
            canvas.StrokeSize = 1;

            for (var step = 0; step <= 4; step++)
            {
                var y = top + chartHeight - (chartHeight * step / 4);
                canvas.DrawLine(left, y, dirtyRect.Width - right, y);
                canvas.DrawString(((int)(maxValue * step / 4)).ToString(), 0, y - 5, left - 5, 10,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            if (_values.Length == 0) return;

            var points = new PointF[_values.Length];
            var spacing = _values.Length > 1 ? chartWidth / (_values.Length - 1) : 0;

            for (var index = 0; index < _values.Length; index++)
            {
                var x = left + (spacing * index);
                var y = top + chartHeight - (_values[index] / maxValue * chartHeight);
                points[index] = new PointF(x, y);
                canvas.DrawString(_labels[index], x - 12, dirtyRect.Height - 16, 24, 12,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            canvas.StrokeColor = Color.FromArgb("#FF5A36");
            canvas.StrokeSize = 2;

            for (var index = 0; index < points.Length - 1; index++)
                canvas.DrawLine(points[index], points[index + 1]);

            canvas.FillColor = Colors.White;

            foreach (var point in points)
            {
                canvas.FillCircle(point, 3);
                canvas.DrawCircle(point, 3);
            }
        }
    }
}
