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

    /// <summary>
    /// Creates a new category inside the given event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        int eventId,
        [FromBody] CreateCategoryRequest request)
    {
        // Load the parent event. We need the OrganiserId from it, so a plain
        // AnyAsync check is not enough here.
        var parentEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        if (parentEvent is null)
        {
            return NotFound(new { message = $"No event was found with ID {eventId}." });
        }

        // Ownership check. Only the organiser who owns the event may add categories to it. 
        // This mirrors the rule already enforced on update and delete, so the whole resource behaves consistently.
        if (parentEvent.OrganiserId != request.OrganiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Only the organiser who owns this event may add categories to it."
            });
        }

        // Duplicate guard. Two races called "Fun Run" in the same event would be impossible to tell apart on an entry form, so we block it.
        // The comparison is case-insensitive because "fun run" and "Fun Run"
        var nameTaken = await _context.Categories
            .AnyAsync(c => c.EventId == eventId
                        && c.CategoryName.ToLower() == request.CategoryName.ToLower());

        if (nameTaken)
        {
            return Conflict(new
            {
                message = $"This event already has a category named '{request.CategoryName}'."
            });
        }

        // Map the contract onto the entity.
        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName,
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        // Build the response from the saved entity. A brand new category has no entrants yet, so EnrolledCount is zero and every space is free.
        var dto = new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants,
            EnrolledCount = 0,
            SpacesRemaining = category.MaxParticipants
        };

        // CreatedAtRoute returns 201 and adds a Location header pointing at the new resource, which is the correct REST response for a create.
        return CreatedAtRoute("GetCategory",
            new { eventId = eventId, categoryId = category.CategoryId },
            dto);
    } 
}
