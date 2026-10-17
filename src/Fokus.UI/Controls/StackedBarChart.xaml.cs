using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Fokus.Core.Models;
using Fokus.Core.Utils;
using Fokus.UI.Utils;

namespace Fokus.UI.Controls;

public partial class StackedBarChart : UserControl
{
    private List<FocusBucket> _buckets = [];
    private int _workdaySeconds = 8 * 3600;

    public StackedBarChart()
    {
        InitializeComponent();
        SizeChanged += (_, _) => RenderChart();
    }

    public void UpdateData(List<FocusBucket> buckets, int workdaySeconds)
    {
        _buckets = buckets;
        _workdaySeconds = workdaySeconds;
        RenderChart();
    }

    private void RenderChart()
    {
        ChartCanvas.Children.Clear();
        LabelsContainer.ItemsSource = null;

        if (_buckets.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        double canvasWidth = ActualWidth;
        double canvasHeight = 145; // Height reserved for bars
        int peak = TimerLogic.PeakSeconds(_buckets, _workdaySeconds);

        double slotWidth = canvasWidth / _buckets.Count;
        double barWidth = Math.Clamp(slotWidth * 0.65, 6, 24);

        // Draw Workday Reference Line if > 0
        if (_workdaySeconds > 0)
        {
            double lineY = canvasHeight - (canvasHeight * _workdaySeconds / peak);
            var refLine = new Line
            {
                X1 = 0,
                Y1 = lineY,
                X2 = canvasWidth,
                Y2 = lineY,
                Stroke = (Brush)Application.Current.FindResource("TextFillColorTertiaryBrush"),
                StrokeDashArray = [3, 3],
                StrokeThickness = 1,
                Opacity = 0.5
            };
            refLine.ToolTip = $"Objetivo de jornada: {TimerLogic.FormatDurationCompact(_workdaySeconds)}";
            ChartCanvas.Children.Add(refLine);
        }

        var labels = new List<string>();

        for (int i = 0; i < _buckets.Count; i++)
        {
            var bucket = _buckets[i];
            DateTime date = TimerLogic.DateFromDayKey(bucket.Day);
            labels.Add(date.Date == DateTime.Today ? "Hoy" : date.ToString("dd/MM"));

            double barX = i * slotWidth + (slotWidth - barWidth) / 2.0;
            double currentY = canvasHeight;

            // Draw stacked segments by task
            foreach (var segment in bucket.Segments)
            {
                double segHeight = (canvasHeight * segment.Seconds) / peak;
                if (segHeight <= 0)
                    continue;

                currentY -= segHeight;
                Color taskColor = ColorHelper.GetTaskColor(segment.Task, true);

                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(2, segHeight),
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = new SolidColorBrush(taskColor),
                    ToolTip = $"{bucket.Day} | {segment.Task}: {TimerLogic.FormatDurationCompact(segment.Seconds)} ({segment.Count} ses.)"
                };

                Canvas.SetLeft(rect, barX);
                Canvas.SetTop(rect, currentY);
                ChartCanvas.Children.Add(rect);
            }

            // Draw Break segment stacked on top
            if (bucket.BreakSeconds > 0)
            {
                double breakHeight = (canvasHeight * bucket.BreakSeconds) / peak;
                currentY -= breakHeight;

                var breakRect = new Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(2, breakHeight),
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(140, 46, 179, 143)),
                    ToolTip = $"{bucket.Day} | Descansos: {TimerLogic.FormatDurationCompact(bucket.BreakSeconds)}"
                };

                Canvas.SetLeft(breakRect, barX);
                Canvas.SetTop(breakRect, currentY);
                ChartCanvas.Children.Add(breakRect);
            }
        }

        LabelsContainer.ItemsSource = labels;
    }
}
