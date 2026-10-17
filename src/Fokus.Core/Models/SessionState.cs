using System.Text.Json.Serialization;

namespace Fokus.Core.Models;

public class SessionState
{
    [JsonPropertyName("stateVal")]
    public int StateVal { get; set; } = 1;

    [JsonPropertyName("initialSeconds")]
    public int InitialSeconds { get; set; }

    [JsonPropertyName("running")]
    public bool Running { get; set; }

    [JsonPropertyName("breakOvertime")]
    public bool BreakOvertime { get; set; }

    [JsonPropertyName("countingUp")]
    public bool CountingUp { get; set; }

    [JsonPropertyName("deadline")]
    public long Deadline { get; set; }

    [JsonPropertyName("remainingMs")]
    public long RemainingMs { get; set; }

    [JsonPropertyName("startedAt")]
    public long StartedAt { get; set; }

    [JsonPropertyName("taskSegmentStart")]
    public long TaskSegmentStart { get; set; }

    [JsonPropertyName("savedAt")]
    public long SavedAt { get; set; }

    [JsonPropertyName("bootId")]
    public string BootId { get; set; } = string.Empty;
}
