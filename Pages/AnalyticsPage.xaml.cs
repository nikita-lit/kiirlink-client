using Microsoft.Maui.Graphics;

namespace KiirLink.Pages;

public partial class AnalyticsPage : ContentPage
{
    public AnalyticsPage()
    {
        InitializeComponent();
        PerformanceChart.Drawable = new PerformanceChartDrawable();
    }

    private sealed class PerformanceChartDrawable : IDrawable
    {
        private static readonly float[] Values = [20, 35, 56, 40, 80, 30, 58];
        private static readonly string[] Days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            const float left = 28;
            const float top = 8;
            const float bottom = 22;
            const float right = 8;

            var chartWidth = dirtyRect.Width - left - right;
            var chartHeight = dirtyRect.Height - top - bottom;

            canvas.FontColor = Color.FromArgb("#969696");
            canvas.FontSize = 8;
            canvas.StrokeColor = Color.FromArgb("#ECECEC");
            canvas.StrokeSize = 1;

            for (var step = 0; step <= 4; step++)
            {
                var y = top + chartHeight - (chartHeight * step / 4);
                canvas.DrawLine(left, y, dirtyRect.Width - right, y);
                canvas.DrawString((step * 20).ToString(), 0, y - 5, left - 5, 10, HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            var points = new PointF[Values.Length];
            var spacing = chartWidth / (Values.Length - 1);

            for (var index = 0; index < Values.Length; index++)
            {
                var x = left + (spacing * index);
                var y = top + chartHeight - (Values[index] / 80f * chartHeight);
                points[index] = new PointF(x, y);
                canvas.DrawString(Days[index], x - 12, dirtyRect.Height - 16, 24, 12, HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            canvas.StrokeColor = Color.FromArgb("#FF5A36");
            canvas.StrokeSize = 2;

            for (var index = 0; index < points.Length - 1; index++)
            {
                canvas.DrawLine(points[index], points[index + 1]);
            }

            canvas.FillColor = Colors.White;

            foreach (var point in points)
            {
                canvas.FillCircle(point, 3);
                canvas.DrawCircle(point, 3);
            }
        }
    }
}
