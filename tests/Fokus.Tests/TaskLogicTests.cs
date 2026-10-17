using Fokus.Core.Models;
using Fokus.Core.Utils;
using Xunit;

namespace Fokus.Tests;

public class TaskLogicTests
{
    [Fact]
    public void AddTask_TrimsAndRejectsBlank()
    {
        var tasks = new List<TaskItem>();
        var item1 = TaskLogic.AddTask(tasks, "   padded   ");
        Assert.NotNull(item1);
        Assert.Equal("padded", item1.Title);
        Assert.False(item1.Done);
        Assert.Equal(1, item1.Id);

        // Blank input must not create a row:
        var blank1 = TaskLogic.AddTask(tasks, "   ");
        Assert.Null(blank1);
        Assert.Single(tasks);

        var blank2 = TaskLogic.AddTask(tasks, "");
        Assert.Null(blank2);
        Assert.Single(tasks);
    }

    [Fact]
    public void Ids_AreUniqueAndStable()
    {
        var tasks = new List<TaskItem>();
        for (int i = 0; i < 4; i++)
        {
            TaskLogic.AddTask(tasks, "task " + i);
        }
        Assert.Equal(4, tasks.Count);

        var ids = tasks.Select(t => t.Id).ToHashSet();
        Assert.Equal(4, ids.Count);

        // Removing item with id 2 and adding another must not reuse id 2:
        TaskLogic.RemoveTask(tasks, 2);
        var added = TaskLogic.AddTask(tasks, "after removal");
        Assert.NotNull(added);
        Assert.Equal(5, added.Id);
    }

    [Fact]
    public void SetDone_AndCountOpen()
    {
        var tasks = new List<TaskItem>();
        TaskLogic.AddTask(tasks, "one");
        TaskLogic.AddTask(tasks, "two");
        Assert.Equal(2, TaskLogic.CountOpen(tasks));

        TaskLogic.SetDone(tasks, 1, true);
        Assert.Equal(1, TaskLogic.CountOpen(tasks));

        TaskLogic.SetDone(tasks, 1, false);
        Assert.Equal(2, TaskLogic.CountOpen(tasks));

        // Unknown id is safe:
        TaskLogic.SetDone(tasks, 999, true);
        Assert.Equal(2, TaskLogic.CountOpen(tasks));
    }

    [Fact]
    public void RemoveTask_AndRemoveDone_ArchiveRatherThanDelete()
    {
        var tasks = new List<TaskItem>();
        TaskLogic.AddTask(tasks, "keep");
        TaskLogic.AddTask(tasks, "finish me");
        TaskLogic.AddTask(tasks, "finish me too");

        TaskLogic.SetDone(tasks, 2, true);
        TaskLogic.SetDone(tasks, 3, true);
        int removedDone = TaskLogic.RemoveDone(tasks);
        Assert.Equal(2, removedDone);

        // Still stored for history:
        Assert.Equal(3, tasks.Count);

        // But visible tasks is only 1:
        var visible = TaskLogic.VisibleTasks(tasks);
        Assert.Single(visible);
        Assert.Equal("keep", visible[0].Title);
        Assert.Equal(1, TaskLogic.CountOpen(tasks));

        // Soft delete the remaining one:
        TaskLogic.RemoveTask(tasks, 1);
        Assert.Empty(TaskLogic.VisibleTasks(tasks));
        Assert.Equal(3, tasks.Count);
    }

    [Fact]
    public void MoveTask_ReordersCorrectly()
    {
        var tasks = new List<TaskItem>();
        TaskLogic.AddTask(tasks, "first");
        TaskLogic.AddTask(tasks, "second");
        TaskLogic.AddTask(tasks, "third");

        // Move "second" up one place:
        bool movedUp = TaskLogic.MoveTask(tasks, 2, -1);
        Assert.True(movedUp);
        Assert.Equal("second", tasks[0].Title);
        Assert.Equal("first", tasks[1].Title);
        Assert.Equal("third", tasks[2].Title);

        // And back down:
        bool movedDown = TaskLogic.MoveTask(tasks, 2, 1);
        Assert.True(movedDown);
        Assert.Equal("first", tasks[0].Title);
        Assert.Equal("second", tasks[1].Title);
    }

    [Fact]
    public void MoveTask_AtTheEndsDoesNothing()
    {
        var tasks = new List<TaskItem>();
        TaskLogic.AddTask(tasks, "first");
        TaskLogic.AddTask(tasks, "second");

        // Already at top, move up:
        Assert.False(TaskLogic.MoveTask(tasks, 1, -1));
        // Already at bottom, move down:
        Assert.False(TaskLogic.MoveTask(tasks, 2, 1));

        // Boundary violations:
        Assert.False(TaskLogic.MoveTask(tasks, 1, -5));
        Assert.False(TaskLogic.MoveTask(tasks, 2, 5));
    }
}
