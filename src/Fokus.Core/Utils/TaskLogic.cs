using Fokus.Core.Models;

namespace Fokus.Core.Utils;

public static class TaskLogic
{
    public static int NextId(IEnumerable<TaskItem> tasks)
    {
        int highest = 0;
        foreach (var task in tasks)
        {
            if (task.Id > highest)
                highest = task.Id;
        }
        return highest + 1;
    }

    public static TaskItem? AddTask(List<TaskItem> tasks, string title)
    {
        string trimmed = (title ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        // If an archived task with the same title exists, restore it
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].Archived && string.Equals(tasks[i].Title, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                tasks[i].Archived = false;
                tasks[i].Done = false;
                return tasks[i];
            }
        }

        var newTask = new TaskItem
        {
            Id = NextId(tasks),
            Title = trimmed,
            Done = false,
            Archived = false
        };
        tasks.Add(newTask);
        return newTask;
    }

    public static bool RenameTask(List<TaskItem> tasks, int id, string title)
    {
        string trimmed = (title ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
            return false;

        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return false;

        task.Title = trimmed;
        return true;
    }

    public static bool RestoreTask(List<TaskItem> tasks, int id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return false;

        task.Archived = false;
        return true;
    }

    public static bool SetDone(List<TaskItem> tasks, int id, bool done)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return false;

        task.Done = done;
        return true;
    }

    public static bool RemoveTask(List<TaskItem> tasks, int id)
    {
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null)
            return false;

        task.Archived = true;
        return true;
    }

    public static int RemoveDone(List<TaskItem> tasks)
    {
        int count = 0;
        foreach (var task in tasks)
        {
            if (task.Done && !task.Archived)
            {
                task.Archived = true;
                count++;
            }
        }
        return count;
    }

    public static List<TaskItem> VisibleTasks(IEnumerable<TaskItem> tasks) =>
        tasks.Where(t => !t.Archived).ToList();

    public static List<TaskItem> ArchivedTasks(IEnumerable<TaskItem> tasks) =>
        tasks.Where(t => t.Archived).ToList();

    public static bool MoveTask(List<TaskItem> tasks, int id, int delta)
    {
        int from = tasks.FindIndex(t => t.Id == id);
        if (from < 0)
            return false;

        int to = from + delta;
        if (to < 0 || to >= tasks.Count)
            return false;

        (tasks[from], tasks[to]) = (tasks[to], tasks[from]);
        return true;
    }

    public static int CountOpen(IEnumerable<TaskItem> tasks) =>
        tasks.Count(t => !t.Done && !t.Archived);
}
