using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Fokus.Core.Models;
using Fokus.Core.Utils;
using Fokus.UI.Utils;

namespace Fokus.UI.Controls;

public partial class DayClockRing : UserControl
{
    public DayClockRing()
    {
        InitializeComponent();
    }

    public void UpdateData(DayTimelineResult timeline, DayBreakdown breakdown)
    {
        TxtTotalFocus.Text = TimerLogic.FormatDurationCompact(breakdown.FocusSeconds);
        TxtSessionsCount.Text = $"{breakdown.Sessions} {(breakdown.Sessions == 1 ? "sesión" : "sesiones")}";

        ClockCanvas.Children.Clear();

        double centerX = 125;
        double centerY = 125;
        double radius = 110;
        double strokeWidth = 14;

        // Draw 24-hour tick marks (0, 6, 12, 18)
        DrawHourTicks(centerX, centerY, radius);

        if (timeline.Blocks.Count == 0)
            return;

        // Map blocks to 24-hour day (86400 seconds)
        // Or to timeline window
        long windowStart = timeline.WindowStart;
        long windowEnd = timeline.WindowEnd;
        long totalWindowMs = Math.Max(1, windowEnd - windowStart);

        foreach (var block in timeline.Blocks)
        {
            double startRatio = Math.Clamp((double)(block.StartMs - windowStart) / totalWindowMs, 0.0, 1.0);
            double endRatio = Math.Clamp((double)(block.EndMs - windowStart) / totalWindowMs, 0.0, 1.0);

            double startAngle = -90.0 + startRatio * 360.0;
            double sweepAngle = Math.Max(1.5, (endRatio - startRatio) * 360.0); // Minimum 1.5 deg visibility

            Color blockColor = SessionKinds.IsFocus(block.Kind)
                ? ColorHelper.GetTaskColor(block.Task, true)
                : Color.FromArgb(140, 46, 179, 143);

            var path = CreateArcPath(centerX, centerY, radius, startAngle, sweepAngle, strokeWidth, new SolidColorBrush(blockColor));
            path.ToolTip = $"{block.Task} ({(SessionKinds.IsFocus(block.Kind) ? "Enfoque" : "Descanso")}): {TimerLogic.FormatDurationCompact((block.EndMs - block.StartMs) / 1000.0)}";
            ClockCanvas.Children.Add(path);
        }
    }

    private void DrawHourTicks(double centerX, double centerY, double radius)
    {
        // Draw 4 subtle tick lines at cardinal positions (top, right, bottom, left)
        double[] angles = [-90, 0, 90, 180];
        string[] labels = ["0h", "6h", "12h", "18h"];

        for (int i = 0; i < angles.Length; i++)
        {
            double rad = angles[i] * Math.PI / 180.0;
            double innerR = radius - 10;
            double outerR = radius + 10;

            var line = new Line
            {
                X1 = centerX + innerR * Math.Cos(rad),
                Y1 = centerY + innerR * Math.Sin(rad),
                X2 = centerX + outerR * Math.Cos(rad),
                Y2 = centerY + outerR * Math.Sin(rad),
                Stroke = (Brush)Application.Current.FindResource("TextFillColorTertiaryBrush"),
                StrokeThickness = 1,
                Opacity = 0.3
            };
            ClockCanvas.Children.Add(line);
        }
    }

    private static System.Windows.Shapes.Path CreateArcPath(
        double centerX,
        double centerY,
        double radius,
        double startAngleDeg,
        double sweepAngleDeg,
        double strokeWidth,
        Brush brush)
    {
        double angle = Math.Clamp(sweepAngleDeg, 0.1, 359.99);
        double startAngleRad = startAngleDeg * Math.PI / 180.0;
        double endAngleRad = (startAngleDeg + angle) * Math.PI / 180.0;

        Point startPoint = new Point(
            centerX + radius * Math.Cos(startAngleRad),
            centerY + radius * Math.Sin(startAngleRad)
        );

        Point endPoint = new Point(
            centerX + radius * Math.Cos(endAngleRad),
            centerY + radius * Math.Sin(endAngleRad)
        );

        var geometry = new PathGeometry();
        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false
        };

        figure.Segments.Add(new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            IsLargeArc = angle > 180.0,
            SweepDirection = SweepDirection.Clockwise
        });

        geometry.Figures.Add(figure);

        return new System.Windows.Shapes.Path
        {
            Data = geometry,
            Stroke = brush,
            StrokeThickness = strokeWidth,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
    }
}
