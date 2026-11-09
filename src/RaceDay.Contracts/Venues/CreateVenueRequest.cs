using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Venues;

// Used for both creating and updating. The id travels in the URL rather
// than the body, so a client can never rename the wrong venue by mistake.
public class CreateVenueRequest
{
    [Required(ErrorMessage = "A venue name is required.")]
    [MaxLength(120)]
    public string VenueName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? AddressLine1 { get; set; }

    [MaxLength(80)]
    public string? City { get; set; }

    [MaxLength(80)]
    public string? Province { get; set; }

    [Range(1, 1000000, ErrorMessage = "Capacity must be at least 1.")]
    public int? Capacity { get; set; }
}