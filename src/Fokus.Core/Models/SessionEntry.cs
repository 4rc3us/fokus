using System.Text.Json.Serialization;

namespace Fokus.Core.Models;

public class SessionEntry
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("at")]
    public long At { get; set; }

    [JsonPropertyName("planned")]
    public int Planned { get; set; }

    [JsonPropertyName("actual")]
    public int Actual { get; set; }

    [JsonPropertyName("task")]
    public string Task { get; set; } = string.Empty;
}
