using System.Text.Json.Serialization;

namespace Fokus.Core.Models;

public record TaskSegment(
    [property: JsonPropertyName("task")] string Task,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("seconds")] int Seconds
);

public record FocusBucket(
    [property: JsonPropertyName("day")] string Day,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("seconds")] int Seconds,
    [property: JsonPropertyName("breakSeconds")] int BreakSeconds,
    [property: JsonPropertyName("segments")] List<TaskSegment> Segments
);

public record DayBreakdown(
    [property: JsonPropertyName("focusSeconds")] int FocusSeconds,
    [property: JsonPropertyName("breakSeconds")] int BreakSeconds,
    [property: JsonPropertyName("idleSeconds")] int IdleSeconds,
    [property: JsonPropertyName("spanSeconds")] int SpanSeconds,
    [property: JsonPropertyName("firstStart")] long FirstStart,
    [property: JsonPropertyName("lastEnd")] long LastEnd,
    [property: JsonPropertyName("sessions")] int Sessions
);

public record PeriodBreakdown(
    [property: JsonPropertyName("focusSeconds")] int FocusSeconds,
    [property: JsonPropertyName("breakSeconds")] int BreakSeconds,
    [property: JsonPropertyName("idleSeconds")] int IdleSeconds,
    [property: JsonPropertyName("spanSeconds")] int SpanSeconds,
    [property: JsonPropertyName("sessions")] int Sessions,
    [property: JsonPropertyName("daysWorked")] int DaysWorked
);

public record TimelineBlock(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("task")] string Task,
    [property: JsonPropertyName("startMs")] long StartMs,
    [property: JsonPropertyName("endMs")] long EndMs
);

public record DayTimelineResult(
    [property: JsonPropertyName("blocks")] List<TimelineBlock> Blocks,
    [property: JsonPropertyName("windowStart")] long WindowStart,
    [property: JsonPropertyName("windowEnd")] long WindowEnd
);
