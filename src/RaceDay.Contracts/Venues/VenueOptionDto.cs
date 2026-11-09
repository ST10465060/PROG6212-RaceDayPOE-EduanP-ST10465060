namespace RaceDay.Contracts.Venues;

// A deliberately tiny version, just for filling the venue dropdown on the
// create event form. No point pulling addresses and capacities over the
// wire when all the select list needs is a value and a label.
public class VenueOptionDto
{
    public int VenueId { get; set; }

    // Pre-joined as "Zwartkops Raceway, Centurion" so the view can bind
    // straight to it without stitching strings together in Razor.
    public string DisplayName { get; set; } = string.Empty;
}