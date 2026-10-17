namespace Fokus.Core.Models;

public static class SessionKinds
{
    public const string Focus = "focus";
    public const string ShortBreak = "shortBreak";
    public const string LongBreak = "longBreak";
    public const string Flow = "flow";
    public const string FlowBreak = "flowBreak";

    public static bool IsFocus(string kind) =>
        kind == Focus || kind == Flow;

    public static bool IsBreak(string kind) =>
        kind == ShortBreak || kind == LongBreak || kind == FlowBreak;
}
