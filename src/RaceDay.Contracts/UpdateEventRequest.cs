using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Events;

// The shape of data an organiser sends when updating an existing event.
public class UpdateEventRequest
{
    [Required(ErrorMessage = "An event name is required.")]
    [StringLength(120, MinimumLength = 3)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "An event date is required.")]
    public DateOnly EventDate { get; set; }

    // Use the typeof(decimal) overload so the bounds are parsed as decimals rather than doubles. 
    [Range(typeof(decimal), "0.1", "999.99",
        ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    public decimal DistanceKm { get; set; }

    // Must match the same allowed values as the Event entity
    [Required(ErrorMessage = "An event type is required.")]
    [RegularExpression("^(Run|Walk|Cycle)$",
        ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A valid venue must be selected.")]
    public int VenueId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A valid organiser must be selected.")]
    public int OrganiserId { get; set; }

    // The organiser controls the event lifecycle
    [Required(ErrorMessage = "A status is required.")]
    [RegularExpression("^(Draft|Open|Closed|Completed|Cancelled)$",
        ErrorMessage = "Status must be Draft, Open, Closed, Completed or Cancelled.")]
    public string Status { get; set; } = string.Empty;
}