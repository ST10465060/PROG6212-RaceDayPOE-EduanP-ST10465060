using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Contracts.Events;
using RaceDay.Contracts.Venues;

namespace RaceDay.Web.Models;

/// <summary>
/// The create and edit forms need more than just the event fields, they
/// also need the venue list to fill the dropdown. 
/// </summary>
public class EventFormViewModel
{
    
    public UpdateEventRequest Event { get; set; } = new();

    // Only set when editing. Null means this is a brand new event.
    public int? EventId { get; set; }

    public List<VenueOptionDto> Venues { get; set; } = new();

    public bool IsEdit => EventId.HasValue;

    // Built once here so the Razor view stays free of hard coded lists.
    public List<SelectListItem> EventTypes { get; } = new()
    {
        new SelectListItem("Run", "Run"),
        new SelectListItem("Walk", "Walk"),
        new SelectListItem("Cycle", "Cycle")
    };

    public List<SelectListItem> Statuses { get; } = new()
    {
        new SelectListItem("Draft, not visible to participants", "Draft"),
        new SelectListItem("Open for entries", "Open"),
        new SelectListItem("Closed, no more entries", "Closed"),
        new SelectListItem("Completed", "Completed"),
        new SelectListItem("Cancelled", "Cancelled")
    };
}