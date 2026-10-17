using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Fokus.UI.Tray;

public static class TrayIconGenerator
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(nint handle);

    public static Icon GenerateIcon(
        int style,
        double progress,
        bool isBreak,
        bool breakOvertime,
        bool flashState,
        string? countdownText = null,
        bool showTimeOnly = false,
        int size = 32)
    {
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.Transparent);

        if (flashState)
        {
            // Dimmed flash frame
            using var flashBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
            g.FillEllipse(flashBrush, 4, 4, size - 8, size - 8);
            return BitmapToIcon(bitmap);
        }

        // Color palettes
        Color primaryColor = isBreak ? Color.FromArgb(46, 179, 143) : Color.FromArgb(235, 75, 85);
        Color dimTrackColor = isBreak ? Color.FromArgb(50, 46, 179, 143) : Color.FromArgb(50, 235, 75, 85);
        Color stemColor = Color.FromArgb(64, 180, 80);

        if (breakOvertime)
        {
            primaryColor = Color.FromArgb(255, 140, 0); // Orange pulse for overrun
        }

        if (showTimeOnly && !string.IsNullOrEmpty(countdownText))
        {
            // Draw compact text inside the 32x32 area
            using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(countdownText, font, textBrush, new RectangleF(0, 0, size, size), format);
            return BitmapToIcon(bitmap);
        }

        float margin = 3f;
        float rectSize = size - margin * 2;
        RectangleF bounds = new RectangleF(margin, margin, rectSize, rectSize);

        float sweepAngle = (float)Math.Clamp(progress * 360.0, 0.0, 360.0);
        float startAngle = -90f; // Start from top

        switch (style)
        {
            case 0: // Tomato Silhouette with progress fill
                DrawTomato(g, bounds, (float)progress, primaryColor, dimTrackColor, stemColor);
                break;

            case 1: // Thin Ring
                using (var trackPen = new Pen(dimTrackColor, 3f))
                {
                    g.DrawEllipse(trackPen, bounds);
                }
                using (var progressPen = new Pen(primaryColor, 3f))
                {
                    progressPen.StartCap = LineCap.Round;
                    progressPen.EndCap = LineCap.Round;
                    if (sweepAngle > 0)
                    {
                        g.DrawArc(progressPen, bounds, startAngle, sweepAngle);
                    }
                }
                break;

            case 2: // Wedge / Pie inside thin outline
            default:
                using (var outlinePen = new Pen(dimTrackColor, 1.5f))
                {
                    g.DrawEllipse(outlinePen, bounds);
                }
                if (sweepAngle > 0)
                {
                    using var fillBrush = new SolidBrush(primaryColor);
                    g.FillPie(fillBrush, bounds.X, bounds.Y, bounds.Width, bounds.Height, startAngle, sweepAngle);
                }
                break;
        }

        return BitmapToIcon(bitmap);
    }

    private static void DrawTomato(
        Graphics g,
        RectangleF bounds,
        float progress,
        Color primaryColor,
        Color dimTrackColor,
        Color stemColor)
    {
        float w = bounds.Width;
        float h = bounds.Height;

        // Tomato body outline
        RectangleF bodyRect = new RectangleF(bounds.X, bounds.Y + h * 0.18f, w, h * 0.82f);

        // Track fill (dim)
        using (var trackBrush = new SolidBrush(dimTrackColor))
        {
            g.FillEllipse(trackBrush, bodyRect);
        }

        // Progress fill (bottom-up clip)
        if (progress > 0)
        {
            float fillH = bodyRect.Height * Math.Clamp(progress, 0f, 1f);
            RectangleF clipRect = new RectangleF(
                bodyRect.X,
                bodyRect.Bottom - fillH,
                bodyRect.Width,
                fillH
            );

            var oldClip = g.Clip;
            g.SetClip(clipRect);
            using (var fillBrush = new SolidBrush(primaryColor))
            {
                g.FillEllipse(fillBrush, bodyRect);
            }
            g.Clip = oldClip;
        }

        // Outer contour line
        using (var borderPen = new Pen(primaryColor, 1.2f))
        {
            g.DrawEllipse(borderPen, bodyRect);
        }

        // Little green stem / calyx on top
        float stemW = w * 0.35f;
        float stemH = h * 0.22f;
        float stemX = bounds.X + (w - stemW) / 2f;
        float stemY = bounds.Y;

        using var stemBrush = new SolidBrush(stemColor);
        PointF[] stemPoints =
        [
            new PointF(stemX, stemY + stemH * 0.7f),
            new PointF(stemX + stemW * 0.3f, stemY + stemH * 0.2f),
            new PointF(stemX + stemW * 0.5f, stemY),
            new PointF(stemX + stemW * 0.7f, stemY + stemH * 0.2f),
            new PointF(stemX + stemW, stemY + stemH * 0.7f),
            new PointF(stemX + stemW * 0.5f, stemY + stemH * 0.45f)
        ];
        g.FillPolygon(stemBrush, stemPoints);
    }

    private static Icon BitmapToIcon(Bitmap bitmap)
    {
        nint hIcon = bitmap.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(hIcon);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }
}
