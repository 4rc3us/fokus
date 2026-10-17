using Fokus.Core.Models;
using Fokus.Core.Utils;

namespace Fokus.Core.Services;

public interface IHistoryManager
{
    event EventHandler? HistoryChanged;

    IReadOnlyList<SessionEntry> Entries { get; }

    void RecordSession(string kind, int plannedSeconds, int actualSeconds, DateTime date, string task, int minSessionMinutes = 5);
    void RenameTaskInHistory(string oldTitle, string newTitle);
    void Prune(DateTime now);

    DayBreakdown GetDayBreakdown(string day);
    PeriodBreakdown GetPeriodBreakdown(int dayCount, DateTime today);
    List<FocusBucket> GetFocusPerDay(int dayCount, DateTime today);
    List<TaskSegment> GetFocusByTask(int dayCount, DateTime today);
    DayTimelineResult GetDayTimeline(string day);
}

public class HistoryManager : IHistoryManager
{
    private readonly IStorageService _storageService;
    private readonly List<SessionEntry> _entries;
    private readonly object _lock = new();

    public event EventHandler? HistoryChanged;

    public HistoryManager(IStorageService storageService)
    {
        _storageService = storageService;
        _entries = _storageService.LoadHistory();
    }

    public IReadOnlyList<SessionEntry> Entries
    {
        get
        {
            lock (_lock)
                return _entries.ToList();
        }
    }

    public void RecordSession(string kind, int plannedSeconds, int actualSeconds, DateTime date, string task, int minSessionMinutes = 5)
    {
        lock (_lock)
        {
            if (!TimerLogic.CountsAsSession(kind, actualSeconds, minSessionMinutes * 60))
                return;

            var entry = TimerLogic.MakeEntry(kind, plannedSeconds, actualSeconds, date, task);
            _entries.Add(entry);
            PruneInternal(date);
            SaveAndNotify();
        }
    }

    public void RenameTaskInHistory(string oldTitle, string newTitle)
    {
        lock (_lock)
        {
            TimerLogic.RenameTaskInHistory(_entries, oldTitle, newTitle);
            SaveAndNotify();
        }
    }

    public void Prune(DateTime now)
    {
        lock (_lock)
        {
            PruneInternal(now);
            SaveAndNotify();
        }
    }

    private void PruneInternal(DateTime now)
    {
        var pruned = TimerLogic.PruneHistory(_entries, now);
        _entries.Clear();
        _entries.AddRange(pruned);
    }

    public DayBreakdown GetDayBreakdown(string day)
    {
        lock (_lock)
            return TimerLogic.GetDayBreakdown(_entries, day);
    }

    public PeriodBreakdown GetPeriodBreakdown(int dayCount, DateTime today)
    {
        lock (_lock)
            return TimerLogic.GetPeriodBreakdown(_entries, dayCount, today);
    }

    public List<FocusBucket> GetFocusPerDay(int dayCount, DateTime today)
    {
        lock (_lock)
            return TimerLogic.FocusPerDay(_entries, dayCount, today);
    }

    public List<TaskSegment> GetFocusByTask(int dayCount, DateTime today)
    {
        lock (_lock)
            return TimerLogic.FocusByTask(_entries, dayCount, today);
    }

    public DayTimelineResult GetDayTimeline(string day)
    {
        lock (_lock)
            return TimerLogic.DayTimeline(_entries, day);
    }

    private void SaveAndNotify()
    {
        _storageService.SaveHistory(_entries);
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }
}
