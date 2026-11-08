using System;

namespace RaceDay.Contracts.Results;

// A single captured result, flattened so the views can bind to it directly.
public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }

    // Kept as a string on the way out too, so Razor can print it without
    // having to format a TimeSpan in the view.
    public string FinishTime { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime RecordedAt { get; set; }
}