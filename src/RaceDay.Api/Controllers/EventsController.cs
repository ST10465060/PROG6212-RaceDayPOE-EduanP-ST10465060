using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services; // Part 3
using RaceDay.Contracts.Events;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    // Both services are handed in by the DI container, which is why they were
    // registered in Program.cs. The controller never creates them itself.
    public EventsController(RaceDayDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Returns all events, optionally filtered by event type or city.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetEvents(
    [FromQuery] string? eventType,
    [FromQuery] string? city,
    [FromQuery] int? organiserId,   // added: an organiser's own events
    [FromQuery] string? status)     // added: the public "Published only" list
    {
        // AsNoTracking because nothing here is going to be updated
        var query = _context.Events
            .AsNoTracking()
            .Include(e => e.Venue)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(e => e.Venue!.City == city);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventSummaryDto
            {
                EventId = e.EventId,
                EventName = e.EventName,
                EventDate = e.EventDate,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                VenueName = e.Venue!.VenueName,
                City = e.Venue.City ?? string.Empty,
                Status = e.Status,
                CategoryCount = e.Categories.Count
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Returns a single event with its venue, organiser and categories.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetEvent(int id)
    {
        var raceEvent = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventId == id)
            .Select(e => new EventDetailDto
            {
                EventId = e.EventId,
                EventName = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                Status = e.Status,
                BannerImageUrl = e.BannerImageUrl,
                VenueName = e.Venue!.VenueName,
                City = e.Venue.City,
                Province = e.Venue.Province,
                OrganiserName = e.Organiser!.FirstName + " " + e.Organiser.LastName,
                Categories = e.Categories
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
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event was found with ID {id}." });
        }

        return Ok(raceEvent);
    }
    /// <summary>
    /// Creates a new event. The venue and organiser must already exist, and the organiser must hold the Organiser role.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDetailDto>> CreateEvent(
        [FromBody] CreateEventRequest request)
    {
        // The event may not be scheduled in the past
        if (request.EventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(request.EventDate),
                "The event date may not be in the past.");
            return ValidationProblem(ModelState);
        }

        var venueExists = await _context.Venues
            .AnyAsync(v => v.VenueId == request.VenueId);

        if (!venueExists)
        {
            ModelState.AddModelError(nameof(request.VenueId),
                $"No venue was found with ID {request.VenueId}.");
            return ValidationProblem(ModelState);
        }

        var organiser = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.OrganiserId);

        if (organiser is null)
        {
            ModelState.AddModelError(nameof(request.OrganiserId),
                $"No user was found with ID {request.OrganiserId}.");
            return ValidationProblem(ModelState);
        }

        // Only an organiser may own an event
        if (organiser.Role != "Organiser")
        {
            ModelState.AddModelError(nameof(request.OrganiserId),
                "The selected user does not have the Organiser role.");
            return ValidationProblem(ModelState);
        }

        var raceEvent = new Event
        {
            EventName = request.EventName.Trim(),
            Description = request.Description?.Trim(),
            EventDate = request.EventDate,
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            VenueId = request.VenueId,
            OrganiserId = request.OrganiserId,
            Status = "Draft"
        };

        _context.Events.Add(raceEvent);
        await _context.SaveChangesAsync();

        // 201 with a Location header pointing at the new resource
        return CreatedAtAction(
            nameof(GetEvent),
            new { id = raceEvent.EventId },
            new { raceEvent.EventId, raceEvent.EventName });
    }
    /// <summary>
    /// Updates an existing event. Only the organiser who owns the event may change it, and the event must still exist.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(
        int id,
        [FromBody] UpdateEventRequest request)
    {
       
        // EF Core must track this entity so that SaveChangesAsync can work out which columns changed and build the UPDATE statement.
        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event was found with ID {id}." });
        }

        // Ownership check: the organiser sending the request must be the one who owns the event.
        if (raceEvent.OrganiserId != request.OrganiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only the organiser who created this event may update it." });
        }

        // A Draft or Open event may not be moved into the past.
        var isUpcoming = raceEvent.Status is "Draft" or "Open";
        if (isUpcoming && request.EventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(request.EventDate),
                "The event date may not be moved into the past.");
            return ValidationProblem(ModelState);
        }

        // The venue may have been changed, so confirm the new one exists
        var venueExists = await _context.Venues
            .AnyAsync(v => v.VenueId == request.VenueId);

        if (!venueExists)
        {
            ModelState.AddModelError(nameof(request.VenueId),
                $"No venue was found with ID {request.VenueId}.");
            return ValidationProblem(ModelState);
        }

        // Overwrite the tracked entity's values. EF Core compares these agains the originals it loaded and writes only the columns that actually changed.
        raceEvent.EventName = request.EventName.Trim();
        raceEvent.Description = request.Description?.Trim();
        raceEvent.EventDate = request.EventDate;
        raceEvent.DistanceKm = request.DistanceKm;
        raceEvent.EventType = request.EventType;
        raceEvent.VenueId = request.VenueId;
        raceEvent.Status = request.Status;

        await _context.SaveChangesAsync();

        return NoContent();
    }
    /// <summary>
    /// Deletes an event. Only the owning organiser may delete it, and an
    /// event that already has participants enrolled may not be removed.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(
        int id,
        [FromQuery] int organiserId)
    {
        // Include the categories and their enrolments so we can check whether
        // anybody has entered this event before we remove it.
        var raceEvent = await _context.Events
            .Include(e => e.Categories)
                .ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event was found with ID {id}." });
        }

        // Ownership check, same rule as the update endpoint
        if (raceEvent.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only the organiser who created this event may delete it." });
        }

        // Business rule: an event that people have entered cannot simply vanish.
        // 409 Conflict means the request is valid, but the current state of the
        // resource does not allow it.
        var hasEnrolments = raceEvent.Categories.Any(c => c.Enrolments.Count > 0);
        if (hasEnrolments)
        {
            return Conflict(new
            {
                message = "This event has participants enrolled and cannot be deleted. " +
                          "Set its status to Cancelled instead."
            });
        }

        _context.Events.Remove(raceEvent);
        await _context.SaveChangesAsync(); 

        return NoContent();
    
    }

    /// <summary>
    /// Uploads or replaces the banner image for an event.
    /// The MVC front end posts the file here; it never speaks to Azure itself.
    /// </summary>
    [HttpPost("{id:int}/banner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var raceEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);

        if (raceEvent is null)
        {
            return NotFound(new { message = $"No event was found with ID {id}." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        // Only allow real image types. Checking the extension keeps someone
        // from uploading an executable and having it served back to browsers.
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowed.Contains(extension))
        {
            return BadRequest(new { message = "Only JPG, PNG or WEBP images are allowed." });
        }

        // Cap the size at 5 MB so one huge upload cannot fill the container.
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            return BadRequest(new { message = "The image may not be larger than 5 MB." });
        }

        // Remove the old banner first, otherwise replaced images pile up
        // in storage forever and nothing ever points at them again.
        if (!string.IsNullOrWhiteSpace(raceEvent.BannerImageUrl))
        {
            await _blobStorage.DeleteAsync(raceEvent.BannerImageUrl);
        }

        await using var stream = file.OpenReadStream();
        var url = await _blobStorage.UploadAsync(stream, file.FileName, file.ContentType);

        raceEvent.BannerImageUrl = url;
        await _context.SaveChangesAsync();

        return Ok(new { bannerImageUrl = url });
    }
}