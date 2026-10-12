namespace RaceDay.Contracts.Events;

public class EventSummaryDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
}