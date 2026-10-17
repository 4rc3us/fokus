using Fokus.Core.Models;
using Fokus.Core.Utils;

namespace Fokus.Core.Services;

public interface ITaskManager
{
    event EventHandler? TasksChanged;

    IReadOnlyList<TaskItem> AllTasks { get; }
    IReadOnlyList<TaskItem> VisibleTasks { get; }
    IReadOnlyList<TaskItem> ArchivedTasks { get; }
    int OpenCount { get; }

    TaskItem? AddTask(string title);
    bool RenameTask(int id, string title);
    bool SetDone(int id, bool done);
    bool RemoveTask(int id);
    bool RestoreTask(int id);
    int RemoveDone();
    bool MoveTask(int id, int delta);
}

public class TaskManager : ITaskManager
{
    private readonly IStorageService _storageService;
    private readonly List<TaskItem> _tasks;
    private readonly object _lock = new();

    public event EventHandler? TasksChanged;

    public TaskManager(IStorageService storageService)
    {
        _storageService = storageService;
        _tasks = _storageService.LoadTasks();
    }

    public IReadOnlyList<TaskItem> AllTasks
    {
        get { lock (_lock) return _tasks.Select(t => t.Clone()).ToList(); }
    }

    public IReadOnlyList<TaskItem> VisibleTasks
    {
        get { lock (_lock) return TaskLogic.VisibleTasks(_tasks).Select(t => t.Clone()).ToList(); }
    }

    public IReadOnlyList<TaskItem> ArchivedTasks
    {
        get { lock (_lock) return TaskLogic.ArchivedTasks(_tasks).Select(t => t.Clone()).ToList(); }
    }

    public int OpenCount
    {
        get { lock (_lock) return TaskLogic.CountOpen(_tasks); }
    }

    public TaskItem? AddTask(string title)
    {
        lock (_lock)
        {
            var task = TaskLogic.AddTask(_tasks, title);
            if (task != null)
            {
                SaveAndNotify();
                return task.Clone();
            }
            return null;
        }
    }

    public bool RenameTask(int id, string title)
    {
        lock (_lock)
        {
            if (TaskLogic.RenameTask(_tasks, id, title))
            {
                SaveAndNotify();
                return true;
            }
            return false;
        }
    }

    public bool SetDone(int id, bool done)
    {
        lock (_lock)
        {
            if (TaskLogic.SetDone(_tasks, id, done))
            {
                SaveAndNotify();
                return true;
            }
            return false;
        }
    }

    public bool RemoveTask(int id)
    {
        lock (_lock)
        {
            if (TaskLogic.RemoveTask(_tasks, id))
            {
                SaveAndNotify();
                return true;
            }
            return false;
        }
    }

    public bool RestoreTask(int id)
    {
        lock (_lock)
        {
            if (TaskLogic.RestoreTask(_tasks, id))
            {
                SaveAndNotify();
                return true;
            }
            return false;
        }
    }

    public int RemoveDone()
    {
        lock (_lock)
        {
            int count = TaskLogic.RemoveDone(_tasks);
            if (count > 0)
            {
                SaveAndNotify();
            }
            return count;
        }
    }

    public bool MoveTask(int id, int delta)
    {
        lock (_lock)
        {
            if (TaskLogic.MoveTask(_tasks, id, delta))
            {
                SaveAndNotify();
                return true;
            }
            return false;
        }
    }

    private void SaveAndNotify()
    {
        _storageService.SaveTasks(_tasks);
        TasksChanged?.Invoke(this, EventArgs.Empty);
    }
}
