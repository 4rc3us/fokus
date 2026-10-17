using Fokus.Core.Models;
using Fokus.Core.Services;
using Xunit;

namespace Fokus.Tests;

public class MockStorageService : IStorageService
{
    public AppSettings Settings { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = [];
    public List<SessionEntry> History { get; set; } = [];
    public SessionState? SessionState { get; set; }

    public AppSettings LoadSettings() => Settings;
    public void SaveSettings(AppSettings settings) => Settings = settings;

    public List<TaskItem> LoadTasks() => Tasks;
    public void SaveTasks(IEnumerable<TaskItem> tasks) => Tasks = tasks.ToList();

    public List<SessionEntry> LoadHistory() => History;
    public void SaveHistory(IEnumerable<SessionEntry> history) => History = history.ToList();

    public SessionState? LoadSessionState() => SessionState;
    public void SaveSessionState(SessionState? state) => SessionState = state;
    public void ClearSessionState() => SessionState = null;
}

public class TimerEngineTests
{
    [Fact]
    public void InitialState_IsCorrect()
    {
        var storage = new MockStorageService();
        var history = new HistoryManager(storage);
        using var engine = new TimerEngine(storage, history);

        engine.Initialize();

        Assert.Equal(1, engine.StateVal);
        Assert.Equal(25 * 60, engine.InitialSeconds);
        Assert.Equal(25 * 60, engine.CounterSeconds);
        Assert.False(engine.IsRunning);
        Assert.False(engine.BreakOvertime);
        Assert.Equal(SessionKinds.Focus, engine.CurrentKind);
        Assert.Equal("25:00", engine.TimeText);
    }

    [Fact]
    public void Skip_AdvancesCycleProperly()
    {
        var storage = new MockStorageService();
        var history = new HistoryManager(storage);
        using var engine = new TimerEngine(storage, history);

        engine.Initialize();
        Assert.Equal(1, engine.StateVal); // Focus 1

        engine.Skip();
        Assert.Equal(2, engine.StateVal); // Short break 1
        Assert.Equal(SessionKinds.ShortBreak, engine.CurrentKind);
        Assert.Equal(5 * 60, engine.InitialSeconds);

        engine.Skip();
        Assert.Equal(3, engine.StateVal); // Focus 2

        engine.Skip();
        Assert.Equal(4, engine.StateVal); // Short break 2

        engine.Skip();
        Assert.Equal(5, engine.StateVal); // Focus 3

        engine.Skip();
        Assert.Equal(6, engine.StateVal); // Short break 3

        engine.Skip();
        Assert.Equal(7, engine.StateVal); // Focus 4

        engine.Skip();
        Assert.Equal(8, engine.StateVal); // Long break
        Assert.Equal(SessionKinds.LongBreak, engine.CurrentKind);
        Assert.Equal(20 * 60, engine.InitialSeconds);

        engine.Skip();
        Assert.Equal(1, engine.StateVal); // Wraps back to Focus 1
    }

    [Fact]
    public void Flowmodoro_InitializesToZero_AndCountsUp()
    {
        var storage = new MockStorageService
        {
            Settings = new AppSettings { FlowmodoroModeEnabled = true, FlowDivisor = 5 }
        };
        var history = new HistoryManager(storage);
        using var engine = new TimerEngine(storage, history);

        engine.Initialize();
        Assert.True(engine.CountingUp);
        Assert.Equal(0, engine.InitialSeconds);
        Assert.Equal(0, engine.CounterSeconds);
        Assert.Equal(SessionKinds.Flow, engine.CurrentKind);
    }

    [Fact]
    public void HistoryManager_DiscardsSessionsShorterThanThreshold()
    {
        var storage = new MockStorageService();
        var history = new HistoryManager(storage);

        // 3 minutes focus session (< 5 min default threshold)
        history.RecordSession(SessionKinds.Focus, 1500, 180, DateTime.Now, "Short Task", minSessionMinutes: 5);
        Assert.Empty(history.Entries);

        // 5 minutes focus session (>= 5 min)
        history.RecordSession(SessionKinds.Focus, 1500, 300, DateTime.Now, "Good Task", minSessionMinutes: 5);
        Assert.Single(history.Entries);

        // Breaks are never filtered even if short (e.g. 2 min)
        history.RecordSession(SessionKinds.ShortBreak, 300, 120, DateTime.Now, "", minSessionMinutes: 5);
        Assert.Equal(2, history.Entries.Count);
    }

    [Fact]
    public void TaskRename_PropagatesToHistory()
    {
        var storage = new MockStorageService();
        var history = new HistoryManager(storage);

        history.RecordSession(SessionKinds.Focus, 1500, 1500, DateTime.Now, "Old Task Name", minSessionMinutes: 1);
        Assert.Equal("Old Task Name", history.Entries[0].Task);

        history.RenameTaskInHistory("Old Task Name", "New Task Name");
        Assert.Equal("New Task Name", history.Entries[0].Task);
    }
}
