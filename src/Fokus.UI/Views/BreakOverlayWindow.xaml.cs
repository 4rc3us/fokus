using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Fokus.Core.Models;
using Fokus.Core.Services;

namespace Fokus.UI.Views;

public partial class BreakOverlayWindow : Window
{
    private readonly ITimerEngine _timerEngine;
    private readonly AppSettings _settings;

    public BreakOverlayWindow(ITimerEngine timerEngine, AppSettings settings)
    {
        InitializeComponent();
        _timerEngine = timerEngine;
        _settings = settings;

        ApplySettings();
        SubscribeEvents();

        Loaded += (_, _) =>
        {
            Focus();
            UpdateFromEngine();
        };

        Closed += (_, _) => UnsubscribeEvents();
    }

    private void ApplySettings()
    {
        // Background Opacity
        double opacity = Math.Clamp(_settings.BreakOverlayOpacity / 100.0, 0.1, 1.0);
        BackdropBorder.Opacity = opacity;

        // Theme palette (0 = Dark, 1 = Light)
        if (_settings.BreakOverlayTheme == 1)
        {
            BackdropBorder.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
        }
        else
        {
            BackdropBorder.Background = new SolidColorBrush(Color.FromRgb(10, 10, 10));
        }

        // Button visibility
        BtnPostpone.Visibility = _settings.FullscreenButtonsPostpone ? Visibility.Visible : Visibility.Collapsed;
        BtnSkip.Visibility = _settings.FullscreenButtonsSkip ? Visibility.Visible : Visibility.Collapsed;
        BtnClose.Visibility = _settings.FullscreenButtonsClose ? Visibility.Visible : Visibility.Collapsed;
        BtnEnd.Visibility = _settings.FullscreenButtonsEnd ? Visibility.Visible : Visibility.Collapsed;

        // Hover visibility option
        if (_settings.ShowButtonsOnHover)
        {
            ButtonsBorder.Opacity = 0.0;
            RootGrid.MouseEnter += (_, _) => ButtonsBorder.Opacity = 1.0;
            RootGrid.MouseLeave += (_, _) => ButtonsBorder.Opacity = 0.0;
        }
    }

    private void SubscribeEvents()
    {
        _timerEngine.Tick += OnEngineTick;
        _timerEngine.StateChanged += OnEngineStateChanged;

        OverlayDial.PlayPauseClick += (_, _) =>
        {
            if (_timerEngine.IsRunning)
                _timerEngine.Pause();
            else
                _timerEngine.Resume();
        };

        OverlayDial.SkipClick += (_, _) => BtnSkip_Click(this, new RoutedEventArgs());
        OverlayDial.StopClick += (_, _) => BtnEnd_Click(this, new RoutedEventArgs());
    }

    private void UnsubscribeEvents()
    {
        _timerEngine.Tick -= OnEngineTick;
        _timerEngine.StateChanged -= OnEngineStateChanged;
    }

    private void OnEngineTick(object? sender, TickEventArgs e)
    {
        Dispatcher.Invoke(UpdateFromEngine);
    }

    private void OnEngineStateChanged(object? sender, StateChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (!e.IsBreak)
            {
                // Break ended and transitioned to focus, close overlay!
                Close();
            }
            else
            {
                UpdateFromEngine();
            }
        });
    }

    private void UpdateFromEngine()
    {
        OverlayDial.TimeText = _timerEngine.TimeText;
        OverlayDial.Progress = _timerEngine.Progress;
        OverlayDial.IsRunning = _timerEngine.IsRunning;
        OverlayDial.IsBreak = true;
        OverlayDial.StatusText = _timerEngine.BreakOvertime ? "Tiempo extra en descanso" : "Descanso";
        OverlayDial.TaskName = _timerEngine.ActiveTask;
        OverlayDial.SessionCount = _settings.NumberOfSessions;
        OverlayDial.SessionIndex = _timerEngine.SessionNumber - 1;
        OverlayDial.IsFlowmodoro = _settings.FlowmodoroModeEnabled;

        if (_timerEngine.BreakOvertime && _settings.FlashOnEnd)
        {
            // Subtle pulse
            OverlayDial.Opacity = OverlayDial.Opacity >= 0.9 ? 0.6 : 1.0;
        }
        else
        {
            OverlayDial.Opacity = 1.0;
        }
    }

    private void BtnPostpone_Click(object sender, RoutedEventArgs e)
    {
        _timerEngine.Postpone(1);
    }

    private void BtnSkip_Click(object sender, RoutedEventArgs e)
    {
        _timerEngine.Skip();
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnEnd_Click(object sender, RoutedEventArgs e)
    {
        _timerEngine.Stop();
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _settings.FullscreenButtonsClose)
        {
            Close();
        }
    }
}
