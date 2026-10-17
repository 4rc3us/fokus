using Fokus.Core.Models;
using Fokus.Core.Utils;
using Xunit;

namespace Fokus.Tests;

public class TimerLogicTests
{
    private readonly DateTime _noon = new(2026, 7, 28, 12, 0, 0);

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(8, true)]
    public void IsBreak_ReturnsExpected(int state, bool expected)
    {
        Assert.Equal(expected, TimerLogic.IsBreak(state));
    }

    [Fact]
    public void IsLongBreak_ReturnsExpected()
    {
        Assert.False(TimerLogic.IsLongBreak(2, 4));
        Assert.False(TimerLogic.IsLongBreak(6, 4));
        Assert.True(TimerLogic.IsLongBreak(8, 4));
        // A single-session run has no short break at all:
        Assert.True(TimerLogic.IsLongBreak(2, 1));
    }

    [Theory]
    [InlineData(1, false, SessionKinds.Focus)]
    [InlineData(2, false, SessionKinds.ShortBreak)]
    [InlineData(8, false, SessionKinds.LongBreak)]
    [InlineData(1, true, SessionKinds.Flow)]
    [InlineData(2, true, SessionKinds.FlowBreak)]
    [InlineData(8, true, SessionKinds.FlowBreak)]
    public void SessionKind_ReturnsExpected(int state, bool flow, string expected)
    {
        Assert.Equal(expected, TimerLogic.GetSessionKind(state, 4, flow));
    }

    [Fact]
    public void NextState_CyclesProperly()
    {
        var seen = new List<int>();
        int s = 1;
        for (int i = 0; i < 8; i++)
        {
            s = TimerLogic.NextState(s, 4);
            seen.Add(s);
        }
        Assert.Equal([2, 3, 4, 5, 6, 7, 8, 1], seen);
    }

    [Fact]
    public void NextState_SkipsShortBreaks()
    {
        Assert.Equal(3, TimerLogic.NextState(1, 4, skipShortBreak: true));
        Assert.Equal(5, TimerLogic.NextState(3, 4, skipShortBreak: true));
        // The long break is not a short break and must still run:
        Assert.Equal(8, TimerLogic.NextState(7, 4, skipShortBreak: true));
    }

    [Fact]
    public void NextState_SkipsLongBreak()
    {
        // Short breaks are untouched:
        Assert.Equal(2, TimerLogic.NextState(1, 4, skipLongBreak: true));
        // And long break wraps back to first focus:
        Assert.Equal(1, TimerLogic.NextState(7, 4, skipLongBreak: true));
    }

    [Fact]
    public void NextState_SkipsBothBreaks()
    {
        Assert.Equal(3, TimerLogic.NextState(1, 4, skipShortBreak: true, skipLongBreak: true));
        Assert.Equal(1, TimerLogic.NextState(7, 4, skipShortBreak: true, skipLongBreak: true));
        // Must terminate even when every break is disabled:
        Assert.Equal(1, TimerLogic.NextState(1, 1, skipShortBreak: true, skipLongBreak: true));
    }

    [Fact]
    public void NextState_FlowmodoroIgnoresBreakSettings()
    {
        Assert.Equal(2, TimerLogic.NextState(1, 4, flowmodoro: true, skipShortBreak: true, skipLongBreak: true));
        Assert.Equal(8, TimerLogic.NextState(7, 4, flowmodoro: true, skipShortBreak: true, skipLongBreak: true));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(7, 4)]
    [InlineData(8, 4)]
    public void SessionNumber_ReturnsExpected(int state, int expected)
    {
        Assert.Equal(expected, TimerLogic.SessionNumber(state));
    }

    [Fact]
    public void PrevState_ReturnsExpected()
    {
        Assert.Equal(3, TimerLogic.PrevState(4));
        Assert.Equal(1, TimerLogic.PrevState(2));
        Assert.Equal(1, TimerLogic.PrevState(1));
    }

    [Theory]
    [InlineData(25, 5, 20, 0)]
    [InlineData(50, 10, 30, 1)]
    [InlineData(90, 20, 30, 2)]
    [InlineData(25, 5, 21, 3)]
    [InlineData(30, 5, 20, 3)]
    [InlineData(13, 3, 7, 3)]
    [InlineData(25, 0, 0, 3)]
    public void MatchPreset_ReturnsExpected(int f, int s, int l, int expected)
    {
        Assert.Equal(expected, PresetConfig.MatchPreset(f, s, l));
    }

    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(9, "00:09")]
    [InlineData(25 * 60, "25:00")]
    [InlineData(59 * 60 + 59, "59:59")]
    [InlineData(3600, "1:00:00")]
    [InlineData(3600 + 5 * 60 + 3, "1:05:03")]
    [InlineData(-1, "-00:01")]
    [InlineData(-83, "-01:23")]
    [InlineData(-60, "-01:00")]
    [InlineData(-(3600 + 60 + 3), "-1:01:03")]
    public void FormatCounter_ReturnsExpected(int seconds, string expected)
    {
        Assert.Equal(expected, TimerLogic.FormatCounter(seconds));
    }

    [Theory]
    [InlineData(1500, "25:00")]
    [InlineData(59, "00:59")]
    [InlineData(0, "00:00")]
    [InlineData(3900, "65:00")]
    [InlineData(7500, "125:00")]
    [InlineData(-300, "-05:00")]
    [InlineData(-59, "-00:59")]
    public void FormatCounterPanel_ReturnsExpected(int seconds, string expected)
    {
        Assert.Equal(expected, TimerLogic.FormatCounterPanel(seconds));
    }

    [Theory]
    [InlineData(0, "0m")]
    [InlineData(45, "1m")]
    [InlineData(1500, "25m")]
    [InlineData(3600, "1h")]
    [InlineData(3900, "1h05")]
    [InlineData(8 * 3600, "8h")]
    [InlineData(49800, "13h50")]
    [InlineData(3599, "1h")]
    [InlineData(-30, "0m")]
    public void FormatDurationCompact_ReturnsExpected(double seconds, string expected)
    {
        Assert.Equal(expected, TimerLogic.FormatDurationCompact(seconds));
    }

    [Theory]
    [InlineData(300, 300, 1.0)]
    [InlineData(150, 300, 0.5)]
    [InlineData(0, 300, 0.0)]
    [InlineData(42, 0, 1.0)]
    [InlineData(400, 300, 1.0)]
    [InlineData(-5, 300, 0.0)]
    public void ProgressFraction_ReturnsExpected(int left, int total, double expected)
    {
        Assert.Equal(expected, TimerLogic.ProgressFraction(left, total), 3);
    }

    [Theory]
    [InlineData(5, 1500, 10, false, true)]
    [InlineData(10, 1500, 10, false, true)]
    [InlineData(11, 1500, 10, false, false)]
    [InlineData(0, 1500, 10, false, false)]
    [InlineData(5, 300, 10, true, false)]
    [InlineData(5, 0, 10, false, false)]
    [InlineData(5, 1500, 0, false, false)]
    [InlineData(-12, 300, 10, true, false)]
    public void ShouldTick_ReturnsExpected(int left, int total, int tick, bool brk, bool expected)
    {
        Assert.Equal(expected, TimerLogic.ShouldTick(left, total, tick, brk));
    }

    [Fact]
    public void TickVolume_RampsUpToFull()
    {
        double first = TimerLogic.TickVolume(10, 10);
        double last = TimerLogic.TickVolume(1, 10);
        Assert.True(first > 0, "first tick must not be silent");
        Assert.Equal(1.0, last);
        Assert.True(last > first);

        for (int s = 10; s > 1; s--)
        {
            Assert.True(TimerLogic.TickVolume(s - 1, 10) > TimerLogic.TickVolume(s, 10));
        }
    }

    [Fact]
    public void IsTimeGap_IdentifiesLargeDowntime()
    {
        Assert.False(TimerLogic.IsTimeGap(250));
        Assert.False(TimerLogic.IsTimeGap(2000));
        Assert.False(TimerLogic.IsTimeGap(60 * 1000));
        Assert.False(TimerLogic.IsTimeGap(5 * 60 * 1000));
        Assert.True(TimerLogic.IsTimeGap(5 * 60 * 1000 + 1));
        Assert.True(TimerLogic.IsTimeGap(9 * 60 * 60 * 1000));
        Assert.False(TimerLogic.IsTimeGap(-3600 * 1000));
    }

    [Fact]
    public void FocusByTask_GroupsAndSortsHeaviestFirst()
    {
        var entries = new List<SessionEntry>
        {
            TimerLogic.MakeEntry(SessionKinds.Focus, 1500, 1500, _noon, "writing"),
            TimerLogic.MakeEntry(SessionKinds.Focus, 1500, 1500, _noon, "writing"),
            TimerLogic.MakeEntry(SessionKinds.Focus, 1500, 600, _noon, "email"),
            TimerLogic.MakeEntry(SessionKinds.ShortBreak, 300, 300, _noon, "writing"),
            TimerLogic.MakeEntry(SessionKinds.Focus, 1500, 900, _noon, "")
        };

        var rows = TimerLogic.FocusByTask(entries, 14, _noon);
        Assert.Equal(3, rows.Count);
        Assert.Equal("writing", rows[0].Task);
        Assert.Equal(2, rows[0].Count);
        Assert.Equal(3000, rows[0].Seconds);

        Assert.Equal("", rows[1].Task);
        Assert.Equal(900, rows[1].Seconds);

        Assert.Equal("email", rows[2].Task);
        Assert.Equal(600, rows[2].Seconds);
    }
}
