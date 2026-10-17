using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using H.NotifyIcon;
using Fokus.Core.Models;
using Fokus.Core.Services;
using Fokus.Core.Utils;
using Fokus.UI.Views;

namespace Fokus.UI.Tray;

public class TrayManager : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private readonly ITimerEngine _timerEngine;
    private readonly ITaskManager _taskManager;
    private readonly IHistoryManager _historyManager;
    private readonly IStorageService _storageService;
    private readonly ISoundService _soundService;
    private readonly IScriptService _scriptService;
    private readonly IMediaControlService _mediaService;
    private readonly IStartupService _startupService;
    private readonly AppSettings _settings;

    private FlyoutWindow? _flyout;
    private readonly List<BreakOverlayWindow> _activeOverlays = [];
    private bool _flashState;
    private int _flashCount;

    public TrayManager(
        ITimerEngine timerEngine,
        ITaskManager taskManager,
        IHistoryManager historyManager,
        IStorageService storageService,
        ISoundService soundService,
        IScriptService scriptService,
        IMediaControlService mediaService,
        IStartupService startupService)
    {
        _timerEngine = timerEngine;
        _taskManager = taskManager;
        _historyManager = historyManager;
        _storageService = storageService;
        _soundService = soundService;
        _scriptService = scriptService;
        _mediaService = mediaService;
        _startupService = startupService;
        _settings = _storageService.LoadSettings();

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Fokus temporizador",
            NoLeftClickDelay = true
        };

        BuildContextMenu();
        SubscribeEvents();
        UpdateTrayVisuals();
    }

    private void SubscribeEvents()
    {
        _trayIcon.TrayLeftMouseDown += OnTrayLeftMouseDown;
        _trayIcon.PreviewMouseWheel += OnTrayMouseWheel;

        _timerEngine.Tick += OnEngineTick;
        _timerEngine.StateChanged += OnEngineStateChanged;
        _timerEngine.SessionCompleted += OnEngineSessionCompleted;
        _timerEngine.TickingAlert += OnEngineTickingAlert;
        _timerEngine.OverrunAlert += OnEngineOverrunAlert;
        _timerEngine.FlowReminderAlert += OnEngineFlowReminderAlert;
    }

    private void BuildContextMenu()
    {
        var menu = new ContextMenu();

        // Presets submenu
        var presetsItem = new MenuItem { Header = "Preajustes rápidos" };

        var p1 = new MenuItem { Header = "25 / 5 / 20 (Clásico)" };
        p1.Click += (_, _) => SetPreset(25, 5, 20);
        presetsItem.Items.Add(p1);

        var p2 = new MenuItem { Header = "50 / 10 / 30 (Trabajo profundo)" };
        p2.Click += (_, _) => SetPreset(50, 10, 30);
        presetsItem.Items.Add(p2);

        var p3 = new MenuItem { Header = "90 / 20 / 30 (Bloque largo)" };
        p3.Click += (_, _) => SetPreset(90, 20, 30);
        presetsItem.Items.Add(p3);

        menu.Items.Add(presetsItem);

        // Flowmodoro toggle
        var flowItem = new MenuItem
        {
            Header = "Modo Flowmodoro",
            IsCheckable = true,
            IsChecked = _settings.FlowmodoroModeEnabled
        };
        flowItem.Click += (_, _) =>
        {
            _settings.FlowmodoroModeEnabled = flowItem.IsChecked;
            _storageService.SaveSettings(_settings);
            _timerEngine.UpdateSettings(_settings);
        };
        menu.Items.Add(flowItem);

        menu.Items.Add(new Separator());

        // Play / Pause
        var playPauseItem = new MenuItem { Header = "Iniciar / Pausar" };
        playPauseItem.Click += (_, _) =>
        {
            if (_timerEngine.IsRunning)
                _timerEngine.Pause();
            else
                _timerEngine.Resume();
        };
        menu.Items.Add(playPauseItem);

        // Skip
        var skipItem = new MenuItem { Header = "Saltar sesión" };
        skipItem.Click += (_, _) => _timerEngine.Skip();
        menu.Items.Add(skipItem);

        // Stop
        var stopItem = new MenuItem { Header = "Detener / Reiniciar" };
        stopItem.Click += (_, _) => _timerEngine.Stop();
        menu.Items.Add(stopItem);

        menu.Items.Add(new Separator());

        // Open window
        var openItem = new MenuItem { Header = "Abrir panel de Fokus" };
        openItem.Click += (_, _) => ShowFlyout();
        menu.Items.Add(openItem);

        // Exit
        var exitItem = new MenuItem { Header = "Salir de Fokus" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
    }

    private void SetPreset(int focus, int shortBreak, int longBreak)
    {
        _settings.FocusTime = focus;
        _settings.ShortBreakTime = shortBreak;
        _settings.LongBreakTime = longBreak;
        _storageService.SaveSettings(_settings);
        _timerEngine.UpdateSettings(_settings);
        UpdateTrayVisuals();
    }

    private void OnTrayLeftMouseDown(object sender, RoutedEventArgs e)
    {
        ShowFlyout();
    }

    private void ShowFlyout()
    {
        if (_flyout == null || !_flyout.IsLoaded)
        {
            _flyout = new FlyoutWindow(
                _timerEngine,
                _taskManager,
                _historyManager,
                _storageService,
                _startupService
            );
        }

        if (_flyout.IsVisible)
        {
            _flyout.Hide();
        }
        else
        {
            var mousePos = GetCursorScreenPosition();
            _flyout.ShowNearTray(mousePos);
        }
    }

    private void OnTrayMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!_settings.ScrollAdjustsTimeEnabled || _settings.FlowmodoroModeEnabled)
            return;

        int delta = e.Delta >= 120 ? 60 : (e.Delta <= -120 ? -60 : 0);
        if (delta != 0)
        {
            _timerEngine.ShiftCounter(delta);
        }
    }

    private void OnEngineTick(object? sender, TickEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(UpdateTrayVisuals);
    }

    private void OnEngineStateChanged(object? sender, StateChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            UpdateTrayVisuals();

            if (e.IsBreak)
            {
                // Play sound if enabled
                if (_settings.BreakStartSfxEnabled)
                    _soundService.PlaySound(_settings.BreakStartSfxFilePath, 1.0, _settings.SoundVolume);

                // Run break start script
                if (_settings.StartBreakScriptEnabled)
                    _scriptService.RunScript(_settings.StartBreakScriptFilePath);

                // Pause active media players
                if (_settings.BreakPauseMediaEnabled)
                    _mediaService.PauseMedia();

                // Show multi-monitor break overlay
                if (_settings.ShowFullscreenBreak)
                    ShowBreakOverlays();
            }
            else
            {
                CloseBreakOverlays();

                if (_settings.TimerStartSfxEnabled)
                    _soundService.PlaySound(_settings.TimerStartSfxFilePath, 1.0, _settings.SoundVolume);

                if (_settings.StartFocusScriptEnabled)
                    _scriptService.RunScript(_settings.StartFocusScriptFilePath);
            }
        });
    }

    private void OnEngineSessionCompleted(object? sender, SessionCompletedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (e.IsBreak)
            {
                if (_settings.BreakEndSfxEnabled)
                    _soundService.PlaySound(_settings.BreakEndSfxFilePath, 1.0, _settings.SoundVolume);

                if (_settings.BreakEndNotificationEnabled)
                    _trayIcon.ShowNotification("Fokus", "¡El descanso ha terminado! Es hora de concentrarse.");

                if (_settings.EndBreakScriptEnabled)
                    _scriptService.RunScript(_settings.EndBreakScriptFilePath);
            }
            else
            {
                if (_settings.TimerStopSfxEnabled)
                    _soundService.PlaySound(_settings.TimerStopSfxFilePath, 1.0, _settings.SoundVolume);

                if (_settings.TimerEndNotificationEnabled)
                    _trayIcon.ShowNotification("Fokus", "¡Sesión de concentración completada! Tómate un respiro.");

                if (_settings.EndFocusScriptEnabled)
                    _scriptService.RunScript(_settings.EndFocusScriptFilePath);
            }

            if (_settings.FlashOnEnd)
            {
                TriggerTrayFlash();
            }
        });
    }

    private void OnEngineTickingAlert(object? sender, TickAlertEventArgs e)
    {
        _soundService.PlayTick(e.VolumeFactor, _settings.SoundVolume);
    }

    private void OnEngineOverrunAlert(object? sender, EventArgs e)
    {
        _soundService.PlayDefaultChime();
    }

    private void OnEngineFlowReminderAlert(object? sender, EventArgs e)
    {
        _trayIcon.ShowNotification("Fokus", $"Llevas {_settings.FlowReminderMinutes} minutos en flujo. ¿Sigues enfocado?");
    }

    private void ShowBreakOverlays()
    {
        CloseBreakOverlays();

        // Enumerate virtual screen or primary
        var overlay = new BreakOverlayWindow(_timerEngine, _settings)
        {
            Left = SystemParameters.VirtualScreenLeft,
            Top = SystemParameters.VirtualScreenTop,
            Width = SystemParameters.VirtualScreenWidth,
            Height = SystemParameters.VirtualScreenHeight
        };
        _activeOverlays.Add(overlay);
        overlay.Show();
    }

    private void CloseBreakOverlays()
    {
        foreach (var overlay in _activeOverlays.ToList())
        {
            try { overlay.Close(); } catch { }
        }
        _activeOverlays.Clear();
    }

    private void TriggerTrayFlash()
    {
        _flashCount = 6;
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        timer.Tick += (s, _) =>
        {
            _flashState = !_flashState;
            _flashCount--;
            UpdateTrayVisuals();

            if (_flashCount <= 0)
            {
                _flashState = false;
                timer.Stop();
                UpdateTrayVisuals();
            }
        };
        timer.Start();
    }

    private void UpdateTrayVisuals()
    {
        bool isBreak = TimerLogic.IsBreak(_timerEngine.StateVal);
        bool showTimeOnly = _settings.SwapIconForTime && _timerEngine.IsRunning;

        var icon = TrayIconGenerator.GenerateIcon(
            _settings.PanelIconStyle,
            _timerEngine.Progress,
            isBreak,
            _timerEngine.BreakOvertime,
            _flashState,
            _timerEngine.PanelTimeText,
            showTimeOnly
        );

        _trayIcon.Icon = icon;

        string statusName = _timerEngine.BreakOvertime
            ? "Tiempo extra"
            : (isBreak ? "Descanso" : (_settings.FlowmodoroModeEnabled ? "Flujo" : "Concentración"));

        string taskInfo = string.IsNullOrWhiteSpace(_timerEngine.ActiveTask) ? "" : $" | {_timerEngine.ActiveTask}";
        string cycleInfo = _settings.FlowmodoroModeEnabled ? "" : $" ({_timerEngine.SessionNumber}/{_settings.NumberOfSessions})";

        _trayIcon.ToolTipText = $"Fokus - {statusName}: {_timerEngine.TimeText}{cycleInfo}{taskInfo}";
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private static Point GetCursorScreenPosition()
    {
        GetCursorPos(out POINT point);
        return new Point(point.X, point.Y);
    }

    public void Dispose()
    {
        CloseBreakOverlays();
        _trayIcon.Dispose();
        _flyout?.Close();
    }
}
