using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts.Categories;
using RaceDay.Contracts.Events;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

// Categories only ever exist inside an event, so the route is nested:
// api/Events/5/Categories. This makes the parent/child relationship obvious from the URL alone and means we never have to guess which event a category belongs to.
[ApiController]
[Route("api/Events/{eventId:int}/Categories")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns every category belonging to the given event.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories(int eventId)
    {
        // Confirm the parent exists first. Without this check an invalid event id would return an empty list, which wrongly implies the event exists but simply has no categories.
        var eventExists = await _context.Events
            .AnyAsync(e => e.EventId == eventId);

        if (!eventExists)
        {
            return NotFound(new { message = $"No event was found with ID {eventId}." });
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                DistanceKm = c.DistanceKm,
                EntryFee = c.EntryFee,
                MaxParticipants = c.MaxParticipants,
                EnrolledCount = c.Enrolments.Count,
                SpacesRemaining = c.MaxParticipants - c.Enrolments.Count
            })
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>
    /// Returns a single category belonging to the given event.
    /// </summary>
    [HttpGet("{categoryId:int}", Name = "GetCategory")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategory(int eventId, int categoryId)
    {
        var category = await _context.Categories
            .AsNoTracking()
            // Both ids must match. Filtering on EventId as well as CategoryId
            // stops someone reading a category through the wrong event's URL.
            .Where(c => c.EventId == eventId && c.CategoryId == categoryId)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                DistanceKm = c.DistanceKm,
                EntryFee = c.EntryFee,
                MaxParticipants = c.MaxParticipants,
                EnrolledCount = c.Enrolments.Count,
                SpacesRemaining = c.MaxParticipants - c.Enrolments.Count
            })
            .FirstOrDefaultAsync();

        if (category is null)
        {
            return NotFound(new
            {
                message = $"No category was found with ID {categoryId} on event {eventId}."
            });
        }

        return Ok(category);
    }
}
