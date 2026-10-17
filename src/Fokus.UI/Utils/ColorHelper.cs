using System.Windows.Media;
using Fokus.Core.Utils;

namespace Fokus.UI.Utils;

public static class ColorHelper
{
    // Generates a pastel, distinct hue for each task, matching focus-plasmoid's taskColor() algorithm
    public static Color GetTaskColor(string? task, bool isDarkTheme = true)
    {
        if (string.IsNullOrWhiteSpace(task))
        {
            // Neutral accent for untracked sessions
            return isDarkTheme ? Color.FromRgb(235, 75, 85) : Color.FromRgb(210, 50, 60);
        }

        int hue = TimerLogic.TaskHue(task);
        if (hue < 0)
            return Color.FromRgb(235, 75, 85);

        // HSL to RGB conversion
        double h = hue / 360.0;
        double s = 0.55;
        double l = isDarkTheme ? 0.65 : 0.48;

        return HslToRgb(h, s, l);
    }

    private static Color HslToRgb(double h, double s, double l)
    {
        double r, g, b;

        if (s == 0.0)
        {
            r = g = b = l;
        }
        else
        {
            double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
            double p = 2.0 * l - q;
            r = HueToRgb(p, q, h + 1.0 / 3.0);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1.0 / 3.0);
        }

        return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0.0) t += 1.0;
        if (t > 1.0) t -= 1.0;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }
}
