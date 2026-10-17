using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Fokus.Core.Models;
using Fokus.Core.Services;
using Fokus.Core.Utils;
using Fokus.UI.Utils;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace Fokus.UI.Views;

public partial class FlyoutWindow : Window
{
    private readonly ITimerEngine _timerEngine;
    private readonly ITaskManager _taskManager;
    private readonly IHistoryManager _historyManager;
    private readonly IStorageService _storageService;
    private readonly IStartupService _startupService;
    private AppSettings _settings;

    private int _selectedPeriodDays = 1;

    public FlyoutWindow(
        ITimerEngine timerEngine,
        ITaskManager taskManager,
        IHistoryManager historyManager,
        IStorageService storageService,
        IStartupService startupService)
    {
        InitializeComponent();
        _timerEngine = timerEngine;
        _taskManager = taskManager;
        _historyManager = historyManager;
        _storageService = storageService;
        _startupService = startupService;
        _settings = _storageService.LoadSettings();

        SubscribeEvents();
        LoadSettingsToUi();
        UpdateDialFromEngine();
        RenderTasksList();
        RenderStats();
    }

    private void SubscribeEvents()
    {
        _timerEngine.Tick += (_, _) => Dispatcher.Invoke(UpdateDialFromEngine);
        _timerEngine.StateChanged += (_, _) => Dispatcher.Invoke(UpdateDialFromEngine);
        _taskManager.TasksChanged += (_, _) => Dispatcher.Invoke(() =>
        {
            RenderTasksList();
            UpdateDialFromEngine();
        });
        _historyManager.HistoryChanged += (_, _) => Dispatcher.Invoke(RenderStats);

        MainDial.PlayPauseClick += (_, _) =>
        {
            if (_timerEngine.IsRunning)
                _timerEngine.Pause();
            else
                _timerEngine.Resume();
        };

        MainDial.StopClick += (_, _) => _timerEngine.Stop();
        MainDial.SkipClick += (_, _) => _timerEngine.Skip();
    }

    public void ShowNearTray(Point trayPosition)
    {
        // Position window just above tray
        var workArea = SystemParameters.WorkArea;

        double left = Math.Clamp(trayPosition.X - (Width / 2.0), 10, workArea.Right - Width - 10);
        double top = workArea.Bottom - Height - 10;

        Left = left;
        Top = top;

        Show();
        Activate();
        Focus();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        Hide();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void UpdateDialFromEngine()
    {
        MainDial.TimeText = _timerEngine.TimeText;
        MainDial.Progress = _timerEngine.Progress;
        MainDial.IsRunning = _timerEngine.IsRunning;
        MainDial.IsBreak = TimerLogic.IsBreak(_timerEngine.StateVal);
        MainDial.StatusText = _timerEngine.BreakOvertime
            ? "Tiempo extra en descanso"
            : (MainDial.IsBreak ? "Descanso" : (_settings.FlowmodoroModeEnabled ? "Flujo de trabajo" : "Concentración"));
        MainDial.TaskName = _timerEngine.ActiveTask;
        MainDial.SessionCount = _settings.NumberOfSessions;
        MainDial.SessionIndex = _timerEngine.SessionNumber - 1;
        MainDial.IsFlowmodoro = _settings.FlowmodoroModeEnabled;

        TxtActiveTaskBar.Text = string.IsNullOrWhiteSpace(_timerEngine.ActiveTask) ? "Sin tarea asignada" : _timerEngine.ActiveTask;

        RbPomodoro.IsChecked = !_settings.FlowmodoroModeEnabled;
        RbFlowmodoro.IsChecked = _settings.FlowmodoroModeEnabled;
        PresetsRow.Visibility = _settings.FlowmodoroModeEnabled ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RbMode_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
            return;

        bool flowMode = RbFlowmodoro.IsChecked == true;
        if (_settings.FlowmodoroModeEnabled != flowMode)
        {
            _settings.FlowmodoroModeEnabled = flowMode;
            _storageService.SaveSettings(_settings);
            _timerEngine.UpdateSettings(_settings);
            UpdateDialFromEngine();
        }
    }

    private void BtnPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender == BtnPreset1)
        {
            _settings.FocusTime = 25;
            _settings.ShortBreakTime = 5;
            _settings.LongBreakTime = 20;
        }
        else if (sender == BtnPreset2)
        {
            _settings.FocusTime = 50;
            _settings.ShortBreakTime = 10;
            _settings.LongBreakTime = 30;
        }
        else if (sender == BtnPreset3)
        {
            _settings.FocusTime = 90;
            _settings.ShortBreakTime = 20;
            _settings.LongBreakTime = 30;
        }

        _storageService.SaveSettings(_settings);
        _timerEngine.UpdateSettings(_settings);
        UpdateDialFromEngine();
    }

    private void BtnSwitchToTasks_Click(object sender, RoutedEventArgs e)
    {
        SwitchTab(TabTasksView, NavBtnTasks);
    }

    #region Tab Navigation
    private void NavBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender == NavBtnTimer) SwitchTab(TabTimerView, NavBtnTimer);
        else if (sender == NavBtnTasks) SwitchTab(TabTasksView, NavBtnTasks);
        else if (sender == NavBtnStats) SwitchTab(TabStatsView, NavBtnStats);
        else if (sender == NavBtnSettings) SwitchTab(TabSettingsView, NavBtnSettings);
    }

    private void SwitchTab(FrameworkElement targetView, Wpf.Ui.Controls.Button activeBtn)
    {
        TabTimerView.Visibility = Visibility.Collapsed;
        TabTasksView.Visibility = Visibility.Collapsed;
        TabStatsView.Visibility = Visibility.Collapsed;
        TabSettingsView.Visibility = Visibility.Collapsed;

        NavBtnTimer.Appearance = ControlAppearance.Transparent;
        NavBtnTasks.Appearance = ControlAppearance.Transparent;
        NavBtnStats.Appearance = ControlAppearance.Transparent;
        NavBtnSettings.Appearance = ControlAppearance.Transparent;

        targetView.Visibility = Visibility.Visible;
        activeBtn.Appearance = ControlAppearance.Secondary;

        if (targetView == TabStatsView)
            RenderStats();
    }
    #endregion

    #region Tasks View Logic
    private void RenderTasksList()
    {
        TaskListContainer.Children.Clear();
        var tasks = _taskManager.VisibleTasks;

        TxtOpenTasksCount.Text = $"{_taskManager.OpenCount} {(_taskManager.OpenCount == 1 ? "tarea pendiente" : "tareas pendientes")}";

        if (tasks.Count == 0)
        {
            TaskListContainer.Children.Add(new TextBlock
            {
                Text = "No hay tareas. ¡Agrega una para comenzar!",
                FontSize = 12,
                Foreground = (Brush)FindResource("TextFillColorTertiaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0)
            });
            return;
        }

        foreach (var task in tasks)
        {
            var taskCard = CreateTaskItemRow(task);
            TaskListContainer.Children.Add(taskCard);
        }
    }

    private FrameworkElement CreateTaskItemRow(TaskItem task)
    {
        var border = new Border
        {
            Background = (Brush)FindResource("CardBackgroundFillColorDefaultBrush"),
            BorderBrush = (Brush)FindResource("CardStrokeColorDefaultBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Checkbox
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Title / Input
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

        // Checkbox
        var cb = new System.Windows.Controls.CheckBox
        {
            IsChecked = task.Done,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        cb.Click += (_, _) => _taskManager.SetDone(task.Id, cb.IsChecked == true);
        Grid.SetColumn(cb, 0);
        grid.Children.Add(cb);

        // Title or Active Chip
        var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var txt = new TextBlock
        {
            Text = task.Title,
            FontSize = 13,
            FontWeight = task.Title == _timerEngine.ActiveTask ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = task.Done ? (Brush)FindResource("TextFillColorTertiaryBrush") : (Brush)FindResource("TextFillColorPrimaryBrush"),
            TextDecorations = task.Done ? TextDecorations.Strikethrough : null,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand
        };
        // Click title to assign as active task!
        txt.MouseLeftButtonDown += (_, _) =>
        {
            _timerEngine.SetTask(task.Title);
            UpdateDialFromEngine();
            RenderTasksList();
        };

        titleStack.Children.Add(txt);
        if (task.Title == _timerEngine.ActiveTask)
        {
            var activeBadge = new TextBlock
            {
                Text = "TAREA ACTIVA",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(235, 75, 85)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(activeBadge);
        }

        Grid.SetColumn(titleStack, 1);
        grid.Children.Add(titleStack);

        // Action Buttons: Up, Down, Delete (Archive)
        var actionPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var btnUp = new Wpf.Ui.Controls.Button
        {
            Icon = new SymbolIcon(SymbolRegular.ArrowUp16),
            Appearance = ControlAppearance.Transparent,
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            ToolTip = "Subir"
        };
        btnUp.Click += (_, _) => _taskManager.MoveTask(task.Id, -1);
        actionPanel.Children.Add(btnUp);

        var btnDown = new Wpf.Ui.Controls.Button
        {
            Icon = new SymbolIcon(SymbolRegular.ArrowDown16),
            Appearance = ControlAppearance.Transparent,
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            ToolTip = "Bajar"
        };
        btnDown.Click += (_, _) => _taskManager.MoveTask(task.Id, 1);
        actionPanel.Children.Add(btnDown);

        var btnDelete = new Wpf.Ui.Controls.Button
        {
            Icon = new SymbolIcon(SymbolRegular.Delete16),
            Appearance = ControlAppearance.Transparent,
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            ToolTip = "Archivar tarea"
        };
        btnDelete.Click += (_, _) =>
        {
            _taskManager.RemoveTask(task.Id);
            if (_timerEngine.ActiveTask == task.Title)
            {
                _timerEngine.SetTask(string.Empty);
            }
        };
        actionPanel.Children.Add(btnDelete);

        Grid.SetColumn(actionPanel, 2);
        grid.Children.Add(actionPanel);

        border.Child = grid;
        return border;
    }

    private void BtnAddTask_Click(object sender, RoutedEventArgs e)
    {
        SubmitNewTask();
    }

    private void TxtNewTask_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            SubmitNewTask();
    }

    private void SubmitNewTask()
    {
        string title = TxtNewTask.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return;

        var added = _taskManager.AddTask(title);
        TxtNewTask.Text = string.Empty;

        if (added != null && string.IsNullOrWhiteSpace(_timerEngine.ActiveTask))
        {
            _timerEngine.SetTask(added.Title);
        }
    }

    private void BtnClearDone_Click(object sender, RoutedEventArgs e)
    {
        _taskManager.RemoveDone();
    }
    #endregion

    #region Stats View Logic
    private void BtnPeriod_Click(object sender, RoutedEventArgs e)
    {
        BtnPeriodToday.Appearance = ControlAppearance.Secondary;
        BtnPeriod7.Appearance = ControlAppearance.Secondary;
        BtnPeriod14.Appearance = ControlAppearance.Secondary;
        BtnPeriod30.Appearance = ControlAppearance.Secondary;

        if (sender == BtnPeriodToday)
        {
            _selectedPeriodDays = 1;
            BtnPeriodToday.Appearance = ControlAppearance.Primary;
        }
        else if (sender == BtnPeriod7)
        {
            _selectedPeriodDays = 7;
            BtnPeriod7.Appearance = ControlAppearance.Primary;
        }
        else if (sender == BtnPeriod14)
        {
            _selectedPeriodDays = 14;
            BtnPeriod14.Appearance = ControlAppearance.Primary;
        }
        else if (sender == BtnPeriod30)
        {
            _selectedPeriodDays = 30;
            BtnPeriod30.Appearance = ControlAppearance.Primary;
        }

        RenderStats();
    }

    private void RenderStats()
    {
        DateTime today = DateTime.Today;
        string todayKey = TimerLogic.DayKey(today);

        var breakdown = _historyManager.GetPeriodBreakdown(_selectedPeriodDays, today);
        TxtMetricFocus.Text = TimerLogic.FormatDurationCompact(breakdown.FocusSeconds);
        TxtMetricBreak.Text = TimerLogic.FormatDurationCompact(breakdown.BreakSeconds);
        TxtMetricIdle.Text = TimerLogic.FormatDurationCompact(breakdown.IdleSeconds);

        if (_selectedPeriodDays == 1)
        {
            // Show 24h clock ring for today
            ClockRing.Visibility = Visibility.Visible;
            StackedChart.Visibility = Visibility.Collapsed;

            var timeline = _historyManager.GetDayTimeline(todayKey);
            var dayBreakdown = _historyManager.GetDayBreakdown(todayKey);
            ClockRing.UpdateData(timeline, dayBreakdown);
        }
        else
        {
            // Show stacked chart for multi-day periods
            ClockRing.Visibility = Visibility.Collapsed;
            StackedChart.Visibility = Visibility.Visible;

            var buckets = _historyManager.GetFocusPerDay(_selectedPeriodDays, today);
            StackedChart.UpdateData(buckets, _settings.WorkdayHours * 3600);
        }

        // Render Task Legend Rows
        TasksLegendContainer.Children.Clear();
        var taskSegments = _historyManager.GetFocusByTask(_selectedPeriodDays, today);

        if (taskSegments.Count > 0)
        {
            TasksLegendContainer.Children.Add(new TextBlock
            {
                Text = "DESGLOSE POR TAREAS",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextFillColorTertiaryBrush"),
                Margin = new Thickness(0, 6, 0, 6)
            });

            foreach (var seg in taskSegments.Take(4))
            {
                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(ColorHelper.GetTaskColor(seg.Task, true)),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(dot, 0);
                row.Children.Add(dot);

                var txtName = new TextBlock
                {
                    Text = string.IsNullOrEmpty(seg.Task) ? "Sin tarea específica" : seg.Task,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("TextFillColorPrimaryBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtName, 1);
                row.Children.Add(txtName);

                var txtTime = new TextBlock
                {
                    Text = $"{TimerLogic.FormatDurationCompact(seg.Seconds)} ({seg.Count} ses.)",
                    FontSize = 12,
                    Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtTime, 2);
                row.Children.Add(txtTime);

                TasksLegendContainer.Children.Add(row);
            }
        }
    }
    #endregion

    #region Settings Logic
    private void LoadSettingsToUi()
    {
        TbFocusTime.Text = _settings.FocusTime.ToString();
        TbShortBreakTime.Text = _settings.ShortBreakTime.ToString();
        TbLongBreakTime.Text = _settings.LongBreakTime.ToString();
        TbSessionsCount.Text = _settings.NumberOfSessions.ToString();
        TbFlowDivisor.Text = _settings.FlowDivisor.ToString();

        SwFullscreenBreak.IsChecked = _settings.ShowFullscreenBreak;
        TbOverlayOpacity.Text = _settings.BreakOverlayOpacity.ToString();
        SwButtonsOnHover.IsChecked = _settings.ShowButtonsOnHover;

        SwNotifyTimerEnd.IsChecked = _settings.TimerEndNotificationEnabled;
        SwNotifyBreakEnd.IsChecked = _settings.BreakEndNotificationEnabled;
        SwSoundTimerEnd.IsChecked = _settings.TimerStopSfxEnabled;
        SwSoundBreakEnd.IsChecked = _settings.BreakEndSfxEnabled;
        SwTicking.IsChecked = _settings.TimerTickSfxEnabled;
        SwPauseMedia.IsChecked = _settings.BreakPauseMediaEnabled;

        CbTrayStyle.SelectedIndex = Math.Clamp(_settings.PanelIconStyle, 0, 2);
        SwAutostart.IsChecked = _startupService.IsAutostartEnabled();
        SwScrollAdjust.IsChecked = _settings.ScrollAdjustsTimeEnabled;
        SwFlashOnEnd.IsChecked = _settings.FlashOnEnd;
    }

    private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TbFocusTime.Text, out int focusTime)) _settings.FocusTime = Math.Max(1, focusTime);
        if (int.TryParse(TbShortBreakTime.Text, out int shortBreak)) _settings.ShortBreakTime = Math.Max(0, shortBreak);
        if (int.TryParse(TbLongBreakTime.Text, out int longBreak)) _settings.LongBreakTime = Math.Max(0, longBreak);
        if (int.TryParse(TbSessionsCount.Text, out int count)) _settings.NumberOfSessions = Math.Clamp(count, 1, 10);
        if (int.TryParse(TbFlowDivisor.Text, out int divisor)) _settings.FlowDivisor = Math.Max(1, divisor);

        _settings.ShowFullscreenBreak = SwFullscreenBreak.IsChecked == true;
        if (int.TryParse(TbOverlayOpacity.Text, out int opacity)) _settings.BreakOverlayOpacity = Math.Clamp(opacity, 10, 100);
        _settings.ShowButtonsOnHover = SwButtonsOnHover.IsChecked == true;

        _settings.TimerEndNotificationEnabled = SwNotifyTimerEnd.IsChecked == true;
        _settings.BreakEndNotificationEnabled = SwNotifyBreakEnd.IsChecked == true;
        _settings.TimerStopSfxEnabled = SwSoundTimerEnd.IsChecked == true;
        _settings.BreakEndSfxEnabled = SwSoundBreakEnd.IsChecked == true;
        _settings.TimerTickSfxEnabled = SwTicking.IsChecked == true;
        _settings.BreakPauseMediaEnabled = SwPauseMedia.IsChecked == true;

        _settings.PanelIconStyle = CbTrayStyle.SelectedIndex;
        _settings.ScrollAdjustsTimeEnabled = SwScrollAdjust.IsChecked == true;
        _settings.FlashOnEnd = SwFlashOnEnd.IsChecked == true;

        _startupService.SetAutostart(SwAutostart.IsChecked == true);

        _storageService.SaveSettings(_settings);
        _timerEngine.UpdateSettings(_settings);

        SwitchTab(TabTimerView, NavBtnTimer);
    }
    #endregion
}
