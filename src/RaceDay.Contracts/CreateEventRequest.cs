using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Events;

public class CreateEventRequest
{
    [Required(ErrorMessage = "An event name is required.")]
    [StringLength(120, MinimumLength = 3)]
    public string EventName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateOnly EventDate { get; set; }

    [Required]
    [Range(0.1, 999.99, ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    public decimal DistanceKm { get; set; }

    // Must be one of Run, Walk or Cycle
    [Required]
    [RegularExpression("^(Run|Walk|Cycle)$",
        ErrorMessage = "Event type must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "A valid venue must be selected.")]
    public int VenueId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "A valid organiser must be selected.")]
    public int OrganiserId { get; set; }
}
