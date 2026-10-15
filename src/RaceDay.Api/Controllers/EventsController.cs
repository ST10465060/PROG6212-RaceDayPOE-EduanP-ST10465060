using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts.Events;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all events, optionally filtered by event type or city.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetEvents(
        [FromQuery] string? eventType,
        [FromQuery] string? city)
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
    /// Creates a new event. The venue and organiser must already exist,
    /// and the organiser must hold the Organiser role.
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
}