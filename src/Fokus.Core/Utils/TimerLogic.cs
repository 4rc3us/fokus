using System.Text.Json;
using Fokus.Core.Models;

namespace Fokus.Core.Utils;

public static class TimerLogic
{
    public const long SessionGapLimitMs = 5 * 60 * 1000; // 5 minutes
    public const long SessionStaleAfterMs = 60 * 60 * 1000; // 60 minutes
    public const int HistoryDays = 30;
    public const int MinTimelineHours = 4;

    public static bool IsBreak(int stateVal) => stateVal % 2 == 0;

    public static bool IsLongBreak(int stateVal, int sessions) => stateVal == 2 * sessions;

    public static string GetSessionKind(int stateVal, int sessions, bool flowmodoro)
    {
        if (flowmodoro)
            return IsBreak(stateVal) ? SessionKinds.FlowBreak : SessionKinds.Flow;
        if (IsLongBreak(stateVal, sessions))
            return SessionKinds.LongBreak;
        if (IsBreak(stateVal))
            return SessionKinds.ShortBreak;
        return SessionKinds.Focus;
    }

    public static int NextState(int stateVal, int sessions, bool flowmodoro = false, bool skipShortBreak = false, bool skipLongBreak = false)
    {
        int next = stateVal < sessions * 2 ? stateVal + 1 : 1;
        if (flowmodoro)
            return next;

        if (IsLongBreak(next, sessions))
        {
            if (skipLongBreak)
                return NextState(next, sessions, false, skipShortBreak, skipLongBreak);
        }
        else if (IsBreak(next) && skipShortBreak)
        {
            return NextState(next, sessions, false, skipShortBreak, skipLongBreak);
        }

        return next;
    }

    public static int PrevState(int stateVal) => stateVal > 1 ? stateVal - 1 : stateVal;

    public static int SessionNumber(int stateVal) => (stateVal + 1) / 2;

    public static bool IsTimeGap(long timeDiffMs) => timeDiffMs > SessionGapLimitMs;

    public static int ElapsedSeconds(int initialSeconds, int counterSeconds, bool countingUp)
    {
        if (countingUp)
            return Math.Max(0, counterSeconds);
        return Math.Max(0, initialSeconds - counterSeconds);
    }

    public static int SegmentSeconds(int elapsed, int segmentStart) =>
        Math.Max(0, elapsed - segmentStart);

    public static bool SessionIsStale(SessionState? session, long nowMs, string bootId)
    {
        if (session == null || session.SavedAt == 0)
            return true;
        if (!string.IsNullOrEmpty(session.BootId) && !string.IsNullOrEmpty(bootId) && session.BootId != bootId)
            return true;
        return nowMs - session.SavedAt > SessionStaleAfterMs;
    }

    public static long RestoredCounterMs(SessionState? session, long nowMs)
    {
        if (session == null)
            return 0;
        if (!session.Running)
            return session.RemainingMs;
        if (session.CountingUp)
            return Math.Max(0, nowMs - session.StartedAt);
        return session.Deadline - nowMs;
    }

    public static int OverrunReminderMs(int seconds)
    {
        if (seconds <= 0)
            return 60 * 1000;
        return Math.Max(10, seconds) * 1000;
    }

    public static int FlowReminderMs(int minutes)
    {
        if (minutes <= 0)
            return 30 * 60 * 1000;
        return Math.Max(1, minutes) * 60 * 1000;
    }

    public static bool CountsAsSession(string kind, int actualSeconds, int minimumSeconds)
    {
        if (!SessionKinds.IsFocus(kind))
            return true;
        return actualSeconds >= minimumSeconds;
    }

    public static string DayKey(DateTime date) => date.ToString("yyyy-MM-dd");

    public static DateTime DateFromDayKey(string day)
    {
        var parts = day.Split('-');
        return new DateTime(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    public static SessionEntry MakeEntry(string kind, int plannedSeconds, int actualSeconds, DateTime date, string task)
    {
        return new SessionEntry
        {
            Kind = kind,
            Day = DayKey(date),
            At = new DateTimeOffset(date).ToUnixTimeMilliseconds(),
            Planned = Math.Max(0, plannedSeconds),
            Actual = Math.Max(0, actualSeconds),
            Task = task ?? string.Empty
        };
    }

    public static List<SessionEntry> PruneHistory(IEnumerable<SessionEntry> entries, DateTime now, int historyDays = HistoryDays)
    {
        long cutoff = new DateTimeOffset(now.AddDays(-historyDays)).ToUnixTimeMilliseconds();
        return entries.Where(e => e.At >= cutoff).ToList();
    }

    public static void RenameTaskInHistory(List<SessionEntry> entries, string oldTitle, string newTitle)
    {
        if (string.IsNullOrEmpty(oldTitle) || oldTitle == newTitle)
            return;

        foreach (var entry in entries)
        {
            if (entry.Task == oldTitle)
                entry.Task = newTitle;
        }
    }

    public static int CountFocusOn(IEnumerable<SessionEntry> entries, string day) =>
        entries.Count(e => e.Day == day && SessionKinds.IsFocus(e.Kind));

    public static int FocusSecondsOn(IEnumerable<SessionEntry> entries, string day) =>
        entries.Where(e => e.Day == day && SessionKinds.IsFocus(e.Kind)).Sum(e => e.Actual);

    public static int BreakSecondsOn(IEnumerable<SessionEntry> entries, string day) =>
        entries.Where(e => e.Day == day && SessionKinds.IsBreak(e.Kind)).Sum(e => e.Actual);

    public static List<(long Start, long End)> MergeIntervals(List<(long Start, long End)> intervals)
    {
        if (intervals.Count == 0)
            return [];

        var sorted = intervals.OrderBy(i => i.Start).ToList();
        var merged = new List<(long Start, long End)> { sorted[0] };

        for (int i = 1; i < sorted.Count; i++)
        {
            var lastIndex = merged.Count - 1;
            var last = merged[lastIndex];
            if (sorted[i].Start <= last.End)
            {
                merged[lastIndex] = (last.Start, Math.Max(last.End, sorted[i].End));
            }
            else
            {
                merged.Add(sorted[i]);
            }
        }

        return merged;
    }

    public static DayBreakdown GetDayBreakdown(IEnumerable<SessionEntry> entries, string day)
    {
        var dayEntries = entries.Where(e => e.Day == day).ToList();
        if (dayEntries.Count == 0)
        {
            return new DayBreakdown(0, 0, 0, 0, 0, 0, 0);
        }

        int focusSeconds = 0;
        int breakSeconds = 0;
        var intervals = new List<(long Start, long End)>();

        foreach (var entry in dayEntries)
        {
            if (SessionKinds.IsFocus(entry.Kind))
                focusSeconds += entry.Actual;
            else if (SessionKinds.IsBreak(entry.Kind))
                breakSeconds += entry.Actual;

            intervals.Add((entry.At - entry.Actual * 1000L, entry.At));
        }

        var merged = MergeIntervals(intervals);
        long firstStart = merged[0].Start;
        long lastEnd = merged[^1].End;

        long coveredMs = merged.Sum(m => m.End - m.Start);
        int spanSeconds = (int)Math.Round((lastEnd - firstStart) / 1000.0);
        int idleSeconds = Math.Max(0, spanSeconds - (int)Math.Round(coveredMs / 1000.0));

        return new DayBreakdown(
            focusSeconds,
            breakSeconds,
            idleSeconds,
            spanSeconds,
            firstStart,
            lastEnd,
            dayEntries.Count
        );
    }

    public static PeriodBreakdown GetPeriodBreakdown(IEnumerable<SessionEntry> entries, int dayCount, DateTime today)
    {
        int focusSeconds = 0;
        int breakSeconds = 0;
        int idleSeconds = 0;
        int spanSeconds = 0;
        int sessions = 0;
        int daysWorked = 0;

        for (int d = dayCount - 1; d >= 0; d--)
        {
            var date = today.Date.AddDays(-d);
            var day = GetDayBreakdown(entries, DayKey(date));
            if (day.Sessions == 0)
                continue;

            focusSeconds += day.FocusSeconds;
            breakSeconds += day.BreakSeconds;
            idleSeconds += day.IdleSeconds;
            spanSeconds += day.SpanSeconds;
            sessions += day.Sessions;
            daysWorked++;
        }

        return new PeriodBreakdown(
            focusSeconds,
            breakSeconds,
            idleSeconds,
            spanSeconds,
            sessions,
            daysWorked
        );
    }

    public static List<TaskSegment> FocusSegmentsOn(IEnumerable<SessionEntry> entries, string day)
    {
        var groups = entries
            .Where(e => e.Day == day && SessionKinds.IsFocus(e.Kind))
            .GroupBy(e => e.Task ?? string.Empty)
            .Select(g => new TaskSegment(g.Key, g.Count(), g.Sum(e => e.Actual)))
            .OrderByDescending(s => s.Seconds)
            .ThenBy(s => s.Task)
            .ToList();

        return groups;
    }

    public static List<FocusBucket> FocusPerDay(IEnumerable<SessionEntry> entries, int dayCount, DateTime today)
    {
        var buckets = new List<FocusBucket>();
        var entryList = entries.ToList();

        for (int d = dayCount - 1; d >= 0; d--)
        {
            var date = today.Date.AddDays(-d);
            var key = DayKey(date);
            buckets.Add(new FocusBucket(
                key,
                CountFocusOn(entryList, key),
                FocusSecondsOn(entryList, key),
                BreakSecondsOn(entryList, key),
                FocusSegmentsOn(entryList, key)
            ));
        }

        return buckets;
    }

    public static List<TaskSegment> FocusByTask(IEnumerable<SessionEntry> entries, int dayCount, DateTime today)
    {
        var cutoff = DayKey(today.Date.AddDays(-(dayCount - 1)));
        var groups = entries
            .Where(e => SessionKinds.IsFocus(e.Kind) && string.CompareOrdinal(e.Day, cutoff) >= 0)
            .GroupBy(e => e.Task ?? string.Empty)
            .Select(g => new TaskSegment(g.Key, g.Count(), g.Sum(e => e.Actual)))
            .OrderByDescending(s => s.Seconds)
            .ThenBy(s => s.Task)
            .ToList();

        return groups;
    }

    public static DayTimelineResult DayTimeline(IEnumerable<SessionEntry> entries, string day)
    {
        var blocks = entries
            .Where(e => e.Day == day)
            .Select(e => new TimelineBlock(e.Kind, e.Task ?? string.Empty, e.At - e.Actual * 1000L, e.At))
            .OrderBy(b => b.StartMs)
            .ToList();

        if (blocks.Count == 0)
            return new DayTimelineResult([], 0, 0);

        long hourMs = 60 * 60 * 1000L;
        long first = blocks[0].StartMs;
        long last = blocks.Max(b => b.EndMs);

        long start = (first / hourMs) * hourMs;
        long end = (long)Math.Ceiling((double)last / hourMs) * hourMs;

        if (end - start < MinTimelineHours * hourMs)
            end = start + MinTimelineHours * hourMs;

        return new DayTimelineResult(blocks, start, end);
    }

    public static int PeakSeconds(IEnumerable<FocusBucket> buckets, int floorSeconds)
    {
        int peak = floorSeconds;
        foreach (var b in buckets)
        {
            int total = b.Seconds + b.BreakSeconds;
            if (total > peak)
                peak = total;
        }
        return Math.Max(1, peak);
    }

    public static int PeakCount(IEnumerable<FocusBucket> buckets)
    {
        int peak = 0;
        foreach (var b in buckets)
        {
            if (b.Count > peak)
                peak = b.Count;
        }
        return Math.Max(1, peak);
    }

    public static string FormatCounter(int totalSeconds)
    {
        string sign = totalSeconds < 0 ? "-" : "";
        int magnitude = Math.Abs(totalSeconds);
        int sec = magnitude % 60;
        int min = (magnitude / 60) % 60;
        int hours = magnitude / 3600;

        if (hours > 0)
            return $"{sign}{hours}:{min:D2}:{sec:D2}";
        return $"{sign}{min:D2}:{sec:D2}";
    }

    public static string FormatCounterPanel(int totalSeconds)
    {
        string sign = totalSeconds < 0 ? "-" : "";
        int magnitude = Math.Abs(totalSeconds);
        int minutes = magnitude / 60;
        int seconds = magnitude % 60;
        return $"{sign}{minutes:D2}:{seconds:D2}";
    }

    public static string FormatDurationCompact(double totalSeconds)
    {
        int seconds = Math.Max(0, (int)Math.Round(totalSeconds));
        int hours = seconds / 3600;
        int minutes = (int)Math.Round((seconds % 3600) / 60.0);

        if (minutes == 60)
        {
            hours++;
            minutes = 0;
        }

        if (hours > 0)
            return minutes > 0 ? $"{hours}h{minutes:D2}" : $"{hours}h";
        return $"{minutes}m";
    }

    public static double ProgressFraction(int counterSeconds, int initialSeconds)
    {
        if (initialSeconds <= 0)
            return 1.0;
        return Math.Clamp((double)counterSeconds / initialSeconds, 0.0, 1.0);
    }

    public static bool ShouldTick(int counterSeconds, int initialSeconds, int tickingSeconds, bool isBreakSession)
    {
        if (isBreakSession || initialSeconds <= 0)
            return false;
        return counterSeconds > 0 && counterSeconds <= tickingSeconds;
    }

    public static double TickVolume(int counterSeconds, int tickingSeconds)
    {
        if (tickingSeconds <= 1)
            return 1.0;
        double elapsed = (tickingSeconds - counterSeconds) / (double)(tickingSeconds - 1);
        elapsed = Math.Clamp(elapsed, 0.0, 1.0);
        return 0.35 + 0.65 * elapsed;
    }

    public static int TaskHue(string? task)
    {
        if (string.IsNullOrEmpty(task))
            return -1; // Neutral / default accent

        int hash = 0;
        foreach (char c in task)
        {
            hash = (hash * 31 + c) % 100000;
        }
        return (hash * 137) % 360;
    }
}
