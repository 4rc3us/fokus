namespace Fokus.Core.Models;

public record PresetConfig(int Focus, int ShortBreak, int LongBreak)
{
    public static readonly PresetConfig[] DefaultPresets =
    [
        new(25, 5, 20),
        new(50, 10, 30),
        new(90, 20, 30)
    ];

    public static int MatchPreset(int focusMinutes, int shortMinutes, int longMinutes)
    {
        for (int i = 0; i < DefaultPresets.Length; i++)
        {
            if (DefaultPresets[i].Focus == focusMinutes &&
                DefaultPresets[i].ShortBreak == shortMinutes &&
                DefaultPresets[i].LongBreak == longMinutes)
            {
                return i;
            }
        }
        return DefaultPresets.Length; // Means Custom
    }
}
