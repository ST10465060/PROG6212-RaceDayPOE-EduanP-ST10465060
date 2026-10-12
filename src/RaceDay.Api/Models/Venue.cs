using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Venue
{
    public int VenueId { get; set; }

    [Required, MaxLength(120)]
    public string VenueName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? AddressLine1 { get; set; }

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? Province { get; set; }

    public int? Capacity { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
