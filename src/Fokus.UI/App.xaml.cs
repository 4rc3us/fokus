using System.Windows;
using Fokus.Core.Services;
using Fokus.UI.Tray;
using Wpf.Ui.Appearance;

namespace Fokus.UI;

public partial class App : Application
{
    private TrayManager? _trayManager;
    private TimerEngine? _timerEngine;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply Windows 11 Dark Fluent Theme
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        // Dependency composition
        var storageService = new StorageService();
        var historyManager = new HistoryManager(storageService);
        var taskManager = new TaskManager(storageService);
        var soundService = new SoundService();
        var scriptService = new ScriptService();
        var mediaService = new MediaControlService();
        var startupService = new StartupService();

        _timerEngine = new TimerEngine(storageService, historyManager);
        _timerEngine.Initialize();

        // Initialize System Tray
        _trayManager = new TrayManager(
            _timerEngine,
            taskManager,
            historyManager,
            storageService,
            soundService,
            scriptService,
            mediaService,
            startupService
        );
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        _timerEngine?.Dispose();
        base.OnExit(e);
    }
}
