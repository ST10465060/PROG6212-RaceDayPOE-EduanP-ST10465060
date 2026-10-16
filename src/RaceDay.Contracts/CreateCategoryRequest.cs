using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Categories;

// The shape of data an organiser sends when adding a category to an event.
// EventId is not included here because it comes from the route (api/Events/{eventId}/Categories), which keeps the URL the single source of truth for which event the category belongs to.
public class CreateCategoryRequest
{
    [Required(ErrorMessage = "A category name is required.")]
    [StringLength(80, MinimumLength = 2,
        ErrorMessage = "Category name must be between 2 and 80 characters.")]
    public string CategoryName { get; set; } = string.Empty;

    // Numeric literals, not strings, so nothing is parsed under the current culture at runtime.
    [Range(0.1, 999.99,
        ErrorMessage = "Distance must be between 0.1 and 999.99 km.")]
    public decimal DistanceKm { get; set; }

    [Range(0, 100000,
        ErrorMessage = "Entry fee must be between 0 and 100000.")]
    public decimal EntryFee { get; set; }

    [Range(1, 100000,
        ErrorMessage = "Maximum participants must be at least 1.")]
    public int MaxParticipants { get; set; }

    // Used to confirm the caller owns the parent event. Replaced by the authenticated user id once JWT authentication is added.
    [Range(1, int.MaxValue, ErrorMessage = "A valid organiser ID is required.")]
    public int OrganiserId { get; set; }
}
