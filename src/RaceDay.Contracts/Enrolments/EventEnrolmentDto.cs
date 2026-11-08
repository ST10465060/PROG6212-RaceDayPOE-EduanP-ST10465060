using System;

namespace RaceDay.Contracts.Enrolments;

// What the organiser sees on the entrant list for one of their events.
public class EventEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public bool HasResult { get; set; }
}
