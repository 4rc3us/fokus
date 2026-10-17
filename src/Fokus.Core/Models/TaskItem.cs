using System.Text.Json.Serialization;

namespace Fokus.Core.Models;

public class TaskItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    public TaskItem Clone() => new()
    {
        Id = Id,
        Title = Title,
        Done = Done,
        Archived = Archived
    };
}
