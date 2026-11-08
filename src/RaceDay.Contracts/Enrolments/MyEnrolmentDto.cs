using System;

namespace RaceDay.Contracts.Enrolments;

// One row in the participant's "my races" list. Flattened on purpose,
// the view needs the event name and date, not a nested object graph.
public class MyEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }

    // Null until the organiser captures a result for this entry.
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
}
