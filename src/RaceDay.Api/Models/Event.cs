using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }

    [Required, MaxLength(120)]
    public string EventName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateOnly EventDate { get; set; }

    // Headline distance advertised for the race
    [Range(0.1, 999.99)]
    public decimal DistanceKm { get; set; }

    // Restricted to Run, Walk or Cycle
    [Required, MaxLength(10)]
    public string EventType { get; set; } = string.Empty;

    public int VenueId { get; set; }
    public Venue? Venue { get; set; }

    public int OrganiserId { get; set; }
    public User? Organiser { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
