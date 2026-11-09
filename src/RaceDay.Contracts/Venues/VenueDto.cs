namespace RaceDay.Contracts.Venues;

// Everything a venue page or an event detail view needs to show.
public class VenueDto
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public int? Capacity { get; set; }

    // Handy for the organiser: a venue that is already in use cannot
    // simply be deleted, and the UI can grey the button out.
    public int EventCount { get; set; }
}
