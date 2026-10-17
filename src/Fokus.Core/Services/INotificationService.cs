namespace Fokus.Core.Services;

public interface INotificationService
{
    void ShowNotification(string title, string message, string? action1Label = null, Action? action1 = null, string? action2Label = null, Action? action2 = null);
}
