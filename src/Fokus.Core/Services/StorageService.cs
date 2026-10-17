using System.IO;
using System.Text.Json;
using Fokus.Core.Models;

namespace Fokus.Core.Services;

public interface IStorageService
{
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);

    List<TaskItem> LoadTasks();
    void SaveTasks(IEnumerable<TaskItem> tasks);

    List<SessionEntry> LoadHistory();
    void SaveHistory(IEnumerable<SessionEntry> history);

    SessionState? LoadSessionState();
    void SaveSessionState(SessionState? state);
    void ClearSessionState();
}

public class StorageService : IStorageService
{
    private readonly string _dataDir;
    private readonly string _settingsFile;
    private readonly string _tasksFile;
    private readonly string _historyFile;
    private readonly string _sessionFile;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public StorageService(string? customDir = null)
    {
        _dataDir = customDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fokus"
        );

        Directory.CreateDirectory(_dataDir);
        _settingsFile = Path.Combine(_dataDir, "settings.json");
        _tasksFile = Path.Combine(_dataDir, "tasks.json");
        _historyFile = Path.Combine(_dataDir, "history.json");
        _sessionFile = Path.Combine(_dataDir, "session.json");
    }

    public AppSettings LoadSettings()
    {
        lock (_lock)
        {
            if (!File.Exists(_settingsFile))
                return new AppSettings();

            try
            {
                string json = File.ReadAllText(_settingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        lock (_lock)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_settingsFile, json);
            }
            catch
            {
                // Silently handle IO exceptions during quick shutdowns
            }
        }
    }

    public List<TaskItem> LoadTasks()
    {
        lock (_lock)
        {
            if (!File.Exists(_tasksFile))
                return [];

            try
            {
                string json = File.ReadAllText(_tasksFile);
                return JsonSerializer.Deserialize<List<TaskItem>>(json, JsonOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public void SaveTasks(IEnumerable<TaskItem> tasks)
    {
        lock (_lock)
        {
            try
            {
                string json = JsonSerializer.Serialize(tasks, JsonOptions);
                File.WriteAllText(_tasksFile, json);
            }
            catch
            {
            }
        }
    }

    public List<SessionEntry> LoadHistory()
    {
        lock (_lock)
        {
            if (!File.Exists(_historyFile))
                return [];

            try
            {
                string json = File.ReadAllText(_historyFile);
                return JsonSerializer.Deserialize<List<SessionEntry>>(json, JsonOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public void SaveHistory(IEnumerable<SessionEntry> history)
    {
        lock (_lock)
        {
            try
            {
                string json = JsonSerializer.Serialize(history, JsonOptions);
                File.WriteAllText(_historyFile, json);
            }
            catch
            {
            }
        }
    }

    public SessionState? LoadSessionState()
    {
        lock (_lock)
        {
            if (!File.Exists(_sessionFile))
                return null;

            try
            {
                string json = File.ReadAllText(_sessionFile);
                return JsonSerializer.Deserialize<SessionState>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }
    }

    public void SaveSessionState(SessionState? state)
    {
        lock (_lock)
        {
            try
            {
                if (state == null)
                {
                    ClearSessionState();
                    return;
                }
                string json = JsonSerializer.Serialize(state, JsonOptions);
                File.WriteAllText(_sessionFile, json);
            }
            catch
            {
            }
        }
    }

    public void ClearSessionState()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_sessionFile))
                    File.Delete(_sessionFile);
            }
            catch
            {
            }
        }
    }
}
