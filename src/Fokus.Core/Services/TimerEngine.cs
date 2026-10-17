using Fokus.Core.Models;
using Fokus.Core.Utils;

namespace Fokus.Core.Services;

public record TickEventArgs(
    int CounterSeconds,
    string TimeText,
    string PanelTimeText,
    double ProgressFraction,
    bool IsBreak,
    bool BreakOvertime,
    bool CountingUp,
    string TaskName
);

public record StateChangedEventArgs(
    int StateVal,
    string Kind,
    int SessionNumber,
    int TotalSessions,
    bool IsBreak,
    bool IsFlowmodoro
);

public record SessionCompletedEventArgs(
    string Kind,
    int PlannedSeconds,
    int ActualSeconds,
    string TaskName,
    bool IsBreak
);

public record TickAlertEventArgs(
    int RemainingSeconds,
    double VolumeFactor
);

public interface ITimerEngine
{
    event EventHandler<TickEventArgs>? Tick;
    event EventHandler<StateChangedEventArgs>? StateChanged;
    event EventHandler<SessionCompletedEventArgs>? SessionCompleted;
    event EventHandler<TickAlertEventArgs>? TickingAlert;
    event EventHandler? OverrunAlert;
    event EventHandler? FlowReminderAlert;

    int StateVal { get; }
    int InitialSeconds { get; }
    int CounterSeconds { get; }
    double CounterMilliseconds { get; }
    string TimeText { get; }
    string PanelTimeText { get; }
    double Progress { get; }
    bool IsRunning { get; }
    bool BreakOvertime { get; }
    bool CountingUp { get; }
    string CurrentKind { get; }
    int SessionNumber { get; }
    string ActiveTask { get; set; }

    void Initialize();
    void Start();
    void Pause();
    void Resume();
    void Stop();
    void Skip();
    void Postpone(int minutes = 1);
    void ShiftCounter(int seconds);
    void SetTask(string newTask);
    void UpdateSettings(AppSettings newSettings);
}

public class TimerEngine : ITimerEngine, IDisposable
{
    private readonly IStorageService _storageService;
    private readonly IHistoryManager _historyManager;
    private AppSettings _settings;
    private readonly object _lock = new();

    private System.Threading.Timer? _loopTimer;
    private long _lastTickWallClock;
    private long _startedAt;
    private long _deadLine;
    private double _counterMilliseconds;
    private int _initialSeconds;
    private int _stateVal = 1;
    private bool _isRunning;
    private bool _breakOvertime;
    private bool _countingUp;
    private long _taskSegmentStart;
    private long _lastOverrunAlertMs;
    private long _lastFlowReminderMs;
    private long _lastSaveTimeMs;

    public event EventHandler<TickEventArgs>? Tick;
    public event EventHandler<StateChangedEventArgs>? StateChanged;
    public event EventHandler<SessionCompletedEventArgs>? SessionCompleted;
    public event EventHandler<TickAlertEventArgs>? TickingAlert;
    public event EventHandler? OverrunAlert;
    public event EventHandler? FlowReminderAlert;

    public TimerEngine(IStorageService storageService, IHistoryManager historyManager)
    {
        _storageService = storageService;
        _historyManager = historyManager;
        _settings = _storageService.LoadSettings();
        ActiveTask = _settings.ActiveTask ?? string.Empty;
    }

    public int StateVal { get { lock (_lock) return _stateVal; } }
    public int InitialSeconds { get { lock (_lock) return _initialSeconds; } }
    public int CounterSeconds { get { lock (_lock) return (int)Math.Ceiling(_counterMilliseconds / 1000.0); } }
    public double CounterMilliseconds { get { lock (_lock) return _counterMilliseconds; } }
    public string TimeText => TimerLogic.FormatCounter(CounterSeconds);
    public string PanelTimeText => TimerLogic.FormatCounterPanel(CounterSeconds);
    public double Progress => TimerLogic.ProgressFraction(CounterSeconds, InitialSeconds);
    public bool IsRunning { get { lock (_lock) return _isRunning; } }
    public bool BreakOvertime { get { lock (_lock) return _breakOvertime; } }
    public bool CountingUp { get { lock (_lock) return _countingUp; } }
    public string CurrentKind => TimerLogic.GetSessionKind(StateVal, _settings.NumberOfSessions, _settings.FlowmodoroModeEnabled);
    public int SessionNumber => TimerLogic.SessionNumber(StateVal);
    public string ActiveTask { get; set; } = string.Empty;

    public void Initialize()
    {
        lock (_lock)
        {
            var saved = _storageService.LoadSessionState();
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Try restoring if not stale
            if (saved != null && !TimerLogic.SessionIsStale(saved, nowMs, GetCurrentBootId()))
            {
                _stateVal = saved.StateVal;
                _initialSeconds = saved.InitialSeconds;
                _breakOvertime = saved.BreakOvertime;
                _countingUp = saved.CountingUp;
                _deadLine = saved.Deadline;
                _startedAt = saved.StartedAt;
                _taskSegmentStart = saved.TaskSegmentStart;

                long restoredMs = TimerLogic.RestoredCounterMs(saved, nowMs);
                _counterMilliseconds = restoredMs;
                _isRunning = saved.Running;

                if (_isRunning)
                {
                    StartTimerLoop();
                }
            }
            else
            {
                ResetToInitial();
            }

            _storageService.ClearSessionState();
        }

        NotifyStateChanged();
        NotifyTick();
    }

    public void UpdateSettings(AppSettings newSettings)
    {
        lock (_lock)
        {
            bool modeChanged = _settings.FlowmodoroModeEnabled != newSettings.FlowmodoroModeEnabled;
            bool focusTimeChanged = _settings.FocusTime != newSettings.FocusTime;
            _settings = newSettings;

            if (!_isRunning || modeChanged || focusTimeChanged)
            {
                Stop();
            }
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _lastTickWallClock = now;
            _countingUp = _settings.FlowmodoroModeEnabled && !TimerLogic.IsBreak(_stateVal);

            if (_counterMilliseconds <= 0 && !_countingUp)
            {
                _initialSeconds = CalculateInitialSeconds();
                _counterMilliseconds = _initialSeconds * 1000.0;
            }

            if (_countingUp)
            {
                _startedAt = now - (long)_counterMilliseconds;
            }
            else
            {
                _deadLine = now + (long)_counterMilliseconds;
            }

            _isRunning = true;
            _breakOvertime = false;
            StartTimerLoop();
            SaveSessionStateInternal();
        }

        NotifyStateChanged();
        NotifyTick();
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (!_isRunning)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_countingUp)
            {
                _counterMilliseconds = Math.Max(0, now - _startedAt);
            }
            else
            {
                _counterMilliseconds = _deadLine - now;
            }

            _isRunning = false;
            StopTimerLoop();
            SaveSessionStateInternal();
        }

        NotifyTick();
    }

    public void Resume()
    {
        Start();
    }

    public void Stop()
    {
        lock (_lock)
        {
            _isRunning = false;
            _breakOvertime = false;
            StopTimerLoop();
            ResetToInitial();
            _storageService.ClearSessionState();
        }

        NotifyStateChanged();
        NotifyTick();
    }

    public void Skip()
    {
        lock (_lock)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int actual = TimerLogic.ElapsedSeconds(_initialSeconds, CounterSeconds, _countingUp);

            // Record stretch if focus
            RecordCurrentSession(actual, now);

            // Advance state
            AdvanceToNextState();
        }

        NotifyStateChanged();
        NotifyTick();
    }

    public void Postpone(int minutes = 1)
    {
        lock (_lock)
        {
            if (!TimerLogic.IsBreak(_stateVal) || _settings.FlowmodoroModeEnabled)
                return;

            int addedSec = minutes * 60;
            _initialSeconds += addedSec;
            _counterMilliseconds += addedSec * 1000.0;

            if (_isRunning)
            {
                _deadLine += addedSec * 1000L;
            }
            _breakOvertime = false;
        }

        NotifyTick();
    }

    public void ShiftCounter(int seconds)
    {
        lock (_lock)
        {
            if (_countingUp || !_settings.ScrollAdjustsTimeEnabled)
                return;

            if (CounterSeconds + seconds > 0)
            {
                _counterMilliseconds += seconds * 1000.0;
                _initialSeconds += seconds;

                if (_isRunning)
                {
                    _deadLine += seconds * 1000L;
                }
            }
        }

        NotifyTick();
    }

    public void SetTask(string newTask)
    {
        lock (_lock)
        {
            if (ActiveTask == newTask)
                return;

            if (_isRunning && SessionKinds.IsFocus(CurrentKind))
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                int elapsed = TimerLogic.ElapsedSeconds(_initialSeconds, CounterSeconds, _countingUp);
                int stretch = TimerLogic.SegmentSeconds(elapsed, (int)_taskSegmentStart);

                if (TimerLogic.CountsAsSession(CurrentKind, stretch, _settings.MinSessionMinutes * 60))
                {
                    _historyManager.RecordSession(
                        CurrentKind,
                        stretch,
                        stretch,
                        DateTime.Now,
                        ActiveTask,
                        _settings.MinSessionMinutes
                    );
                }

                _taskSegmentStart = elapsed;
            }

            ActiveTask = newTask;
            _settings.ActiveTask = newTask;
            _storageService.SaveSettings(_settings);
        }

        NotifyTick();
    }

    private void AdvanceToNextState()
    {
        _stateVal = TimerLogic.NextState(
            _stateVal,
            _settings.NumberOfSessions,
            _settings.FlowmodoroModeEnabled,
            _settings.ShortBreakTime == 0,
            _settings.LongBreakTime == 0
        );

        _breakOvertime = false;
        _initialSeconds = CalculateInitialSeconds();
        _counterMilliseconds = _initialSeconds * 1000.0;
        _taskSegmentStart = 0;

        bool isBreak = TimerLogic.IsBreak(_stateVal);
        bool shouldAutoStart = isBreak ? _settings.TimerAutoFocusEnabled : !_settings.TimerAutoPauseEnabled;

        if (shouldAutoStart)
        {
            _countingUp = _settings.FlowmodoroModeEnabled && !isBreak;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_countingUp)
                _startedAt = now;
            else
                _deadLine = now + (long)_counterMilliseconds;

            _isRunning = true;
            StartTimerLoop();
        }
        else
        {
            _isRunning = false;
            StopTimerLoop();
        }

        SaveSessionStateInternal();
    }

    private int CalculateInitialSeconds()
    {
        if (_settings.FlowmodoroModeEnabled && !TimerLogic.IsBreak(_stateVal))
        {
            return 0;
        }

        if (TimerLogic.IsLongBreak(_stateVal, _settings.NumberOfSessions))
        {
            return _settings.LongBreakTime * 60;
        }

        if (TimerLogic.IsBreak(_stateVal))
        {
            if (_settings.FlowmodoroModeEnabled)
            {
                // Derived from previous flow duration / flow_divisor
                int divisor = Math.Max(1, _settings.FlowDivisor);
                int lastFlowSeconds = Math.Max(0, (int)_taskSegmentStart);
                return Math.Max(60, lastFlowSeconds / divisor);
            }
            return _settings.ShortBreakTime * 60;
        }

        return _settings.FocusTime * 60;
    }

    private void ResetToInitial()
    {
        _stateVal = 1;
        _countingUp = _settings.FlowmodoroModeEnabled;
        _initialSeconds = CalculateInitialSeconds();
        _counterMilliseconds = _initialSeconds * 1000.0;
        _taskSegmentStart = 0;
    }

    private void RecordCurrentSession(int actualSeconds, long nowMs)
    {
        int planned = _initialSeconds;
        int stretch = TimerLogic.SegmentSeconds(actualSeconds, (int)_taskSegmentStart);

        _historyManager.RecordSession(
            CurrentKind,
            planned,
            stretch,
            DateTime.Now,
            ActiveTask,
            _settings.MinSessionMinutes
        );

        SessionCompleted?.Invoke(this, new SessionCompletedEventArgs(
            CurrentKind,
            planned,
            stretch,
            ActiveTask,
            TimerLogic.IsBreak(_stateVal)
        ));
    }

    private void TimerLoopCallback(object? state)
    {
        int prevSec;
        bool fireTickAlert = false;
        double volumeFactor = 1.0;
        bool fireOverrun = false;
        bool fireFlowReminder = false;
        bool sessionFinished = false;

        lock (_lock)
        {
            if (!_isRunning)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long diff = now - _lastTickWallClock;
            _lastTickWallClock = now;

            // Check for suspension / sleep gap
            if (TimerLogic.IsTimeGap(diff))
            {
                // Shift deadline / startedAt to ignore sleep gap
                if (_countingUp)
                    _startedAt += diff;
                else
                    _deadLine += diff;
            }

            prevSec = CounterSeconds;

            if (_countingUp)
            {
                _counterMilliseconds = Math.Max(0, now - _startedAt);
            }
            else
            {
                _counterMilliseconds = _deadLine - now;
            }

            int curSec = CounterSeconds;

            // Check second boundary crossing
            if (curSec != prevSec)
            {
                // Approaching deadline tick check
                if (TimerLogic.ShouldTick(curSec, _initialSeconds, _settings.TickingTime, TimerLogic.IsBreak(_stateVal)))
                {
                    fireTickAlert = true;
                    volumeFactor = TimerLogic.TickVolume(curSec, _settings.TickingTime);
                }
            }

            // Countdown reached zero
            if (!_countingUp && _counterMilliseconds <= 0)
            {
                if (TimerLogic.IsBreak(_stateVal))
                {
                    // Overrun mode
                    if (!_breakOvertime)
                    {
                        _breakOvertime = true;
                        _lastOverrunAlertMs = now;
                        sessionFinished = true;
                    }
                    else if (_settings.BreakOverrunRepeatEnabled)
                    {
                        int repeatMs = TimerLogic.OverrunReminderMs(_settings.BreakOverrunRepeatSeconds);
                        if (now - _lastOverrunAlertMs >= repeatMs)
                        {
                            _lastOverrunAlertMs = now;
                            fireOverrun = true;
                        }
                    }
                }
                else
                {
                    // Focus session finished
                    sessionFinished = true;
                }
            }

            // Flow reminder check
            if (_countingUp && _settings.FlowReminderEnabled)
            {
                int reminderMs = TimerLogic.FlowReminderMs(_settings.FlowReminderMinutes);
                if (now - _startedAt >= reminderMs && now - _lastFlowReminderMs >= reminderMs)
                {
                    _lastFlowReminderMs = now;
                    fireFlowReminder = true;
                }
            }

            // Periodic keepalive save
            if (now - _lastSaveTimeMs > 20000)
            {
                _lastSaveTimeMs = now;
                SaveSessionStateInternal();
            }
        }

        if (fireTickAlert && _settings.TimerTickSfxEnabled)
        {
            TickingAlert?.Invoke(this, new TickAlertEventArgs(CounterSeconds, volumeFactor));
        }

        if (fireOverrun)
        {
            OverrunAlert?.Invoke(this, EventArgs.Empty);
        }

        if (fireFlowReminder)
        {
            FlowReminderAlert?.Invoke(this, EventArgs.Empty);
        }

        if (sessionFinished)
        {
            lock (_lock)
            {
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                int actual = TimerLogic.ElapsedSeconds(_initialSeconds, CounterSeconds, _countingUp);
                RecordCurrentSession(actual, now);

                if (!TimerLogic.IsBreak(_stateVal))
                {
                    // Advance to break
                    if (_countingUp)
                        _taskSegmentStart = actual; // To calculate flow break duration

                    AdvanceToNextState();
                }
            }
            NotifyStateChanged();
        }

        NotifyTick();
    }

    private void StartTimerLoop()
    {
        _lastTickWallClock = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _loopTimer?.Dispose();
        _loopTimer = new System.Threading.Timer(TimerLoopCallback, null, 0, 200);
    }

    private void StopTimerLoop()
    {
        _loopTimer?.Dispose();
        _loopTimer = null;
    }

    private void SaveSessionStateInternal()
    {
        var state = new SessionState
        {
            StateVal = _stateVal,
            InitialSeconds = _initialSeconds,
            Running = _isRunning,
            BreakOvertime = _breakOvertime,
            CountingUp = _countingUp,
            Deadline = _deadLine,
            RemainingMs = (long)_counterMilliseconds,
            StartedAt = _startedAt,
            TaskSegmentStart = _taskSegmentStart,
            SavedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            BootId = GetCurrentBootId()
        };
        _storageService.SaveSessionState(state);
    }

    private static string GetCurrentBootId()
    {
        return Environment.TickCount64.ToString();
    }

    private void NotifyTick()
    {
        Tick?.Invoke(this, new TickEventArgs(
            CounterSeconds,
            TimeText,
            PanelTimeText,
            Progress,
            TimerLogic.IsBreak(StateVal),
            BreakOvertime,
            CountingUp,
            ActiveTask
        ));
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, new StateChangedEventArgs(
            StateVal,
            CurrentKind,
            SessionNumber,
            _settings.NumberOfSessions,
            TimerLogic.IsBreak(StateVal),
            _settings.FlowmodoroModeEnabled
        ));
    }

    public void Dispose()
    {
        StopTimerLoop();
    }
}
