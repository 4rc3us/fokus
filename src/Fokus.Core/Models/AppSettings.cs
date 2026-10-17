namespace Fokus.Core.Models;

public class AppSettings
{
    // Timer intervals
    public int FocusTime { get; set; } = 25;
    public int ShortBreakTime { get; set; } = 5;
    public int LongBreakTime { get; set; } = 20;
    public int TickingTime { get; set; } = 10;
    public int NumberOfSessions { get; set; } = 4;

    // Flowmodoro
    public bool FlowmodoroModeEnabled { get; set; } = false;
    public int FlowDivisor { get; set; } = 5;
    public bool FlowReminderEnabled { get; set; } = false;
    public int FlowReminderMinutes { get; set; } = 30;

    // Font & Display
    public string ClockFontFamily { get; set; } = string.Empty;
    public bool ClockBold { get; set; } = false;
    public bool ShowDialTime { get; set; } = true;
    public bool ScrollAdjustsTimeEnabled { get; set; } = true;

    // Notifications
    public bool TimerStartNotificationEnabled { get; set; } = false;
    public bool TimerEndNotificationEnabled { get; set; } = true;
    public bool BreakStartNotificationEnabled { get; set; } = true;
    public bool BreakEndNotificationEnabled { get; set; } = true;

    // Sound effects
    public bool TimerStartSfxEnabled { get; set; } = false;
    public string TimerStartSfxFilePath { get; set; } = string.Empty;
    public bool TimerStopSfxEnabled { get; set; } = true;
    public string TimerStopSfxFilePath { get; set; } = string.Empty;
    public bool BreakStartSfxEnabled { get; set; } = false;
    public string BreakStartSfxFilePath { get; set; } = string.Empty;
    public bool BreakEndSfxEnabled { get; set; } = true;
    public string BreakEndSfxFilePath { get; set; } = string.Empty;

    public bool BreakOverrunRepeatEnabled { get; set; } = true;
    public int BreakOverrunRepeatSeconds { get; set; } = 60;
    public int SoundVolume { get; set; } = 100;

    public bool TimerTickSfxEnabled { get; set; } = false;
    public string TimerTickSfxFilePath { get; set; } = string.Empty;

    // Automation
    public bool TimerAutoFocusEnabled { get; set; } = false;
    public bool TimerAutoPauseEnabled { get; set; } = false;
    public bool BreakPauseMediaEnabled { get; set; } = false;
    public bool DoNotDisturbEnabled { get; set; } = false;
    public bool Autostart { get; set; } = false;

    // Scripts
    public string StartFocusScriptFilePath { get; set; } = string.Empty;
    public bool StartFocusScriptEnabled { get; set; } = false;
    public string StartBreakScriptFilePath { get; set; } = string.Empty;
    public bool StartBreakScriptEnabled { get; set; } = false;
    public string EndFocusScriptFilePath { get; set; } = string.Empty;
    public bool EndFocusScriptEnabled { get; set; } = false;
    public string EndBreakScriptFilePath { get; set; } = string.Empty;
    public bool EndBreakScriptEnabled { get; set; } = false;
    public string StopScriptFilePath { get; set; } = string.Empty;
    public bool StopScriptEnabled { get; set; } = false;

    // Tray & Panel
    public bool ShowIconInCompactMode { get; set; } = true;
    public int PanelIconStyle { get; set; } = 0; // 0 = tomato, 1 = ring, 2 = wedge
    public bool ShowTimeInCompactMode { get; set; } = false;
    public bool SwapIconForTime { get; set; } = false;
    public bool FlashOnEnd { get; set; } = true;

    // Fullscreen Break Overlay
    public bool ShowFullscreenBreak { get; set; } = true;
    public int BreakOverlayTheme { get; set; } = 0; // 0 = Dark, 1 = Light
    public bool BreakOverlayBlur { get; set; } = true;
    public int BreakOverlayOpacity { get; set; } = 65;
    public bool ShowButtonsOnHover { get; set; } = false;
    public bool FullscreenButtonsPostpone { get; set; } = true;
    public bool FullscreenButtonsSkip { get; set; } = true;
    public bool FullscreenButtonsClose { get; set; } = true;
    public bool FullscreenButtonsEnd { get; set; } = true;

    // Stats
    public int WorkdayHours { get; set; } = 8;
    public int MinSessionMinutes { get; set; } = 5;

    // Active Task
    public string ActiveTask { get; set; } = string.Empty;
}
