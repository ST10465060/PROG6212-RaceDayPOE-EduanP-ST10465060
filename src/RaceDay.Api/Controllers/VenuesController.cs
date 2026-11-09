using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts.Venues;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public VenuesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// All venues, alphabetically, with a count of the events held at each.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VenueDto>>> GetAll()
    {
        var venues = await _context.Venues
            .OrderBy(v => v.VenueName)
            .Select(v => new VenueDto
            {
                VenueId = v.VenueId,
                VenueName = v.VenueName,
                AddressLine1 = v.AddressLine1,
                City = v.City,
                Province = v.Province,
                Capacity = v.Capacity,
                EventCount = v.Events.Count
            })
            .ToListAsync();

        return Ok(venues);
    }

    /// <summary>
    /// The cut down list used to populate the venue dropdown on the
    /// create event form.
    /// </summary>
    [HttpGet("options")]
    public async Task<ActionResult<IEnumerable<VenueOptionDto>>> GetOptions()
    {
        var options = await _context.Venues
            .OrderBy(v => v.VenueName)
            .Select(v => new VenueOptionDto
            {
                VenueId = v.VenueId,

                // Built in the query so SQL does the work. The null check
                // keeps venues without a city from reading "Name, ".
                DisplayName = v.City != null
                    ? v.VenueName + ", " + v.City
                    : v.VenueName
            })
            .ToListAsync();

        return Ok(options);
    }

    /// <summary>
    /// One venue by id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<VenueDto>> GetById(int id)
    {
        var venue = await _context.Venues
            .Where(v => v.VenueId == id)
            .Select(v => new VenueDto
            {
                VenueId = v.VenueId,
                VenueName = v.VenueName,
                AddressLine1 = v.AddressLine1,
                City = v.City,
                Province = v.Province,
                Capacity = v.Capacity,
                EventCount = v.Events.Count
            })
            .FirstOrDefaultAsync();

        if (venue is null)
        {
            return NotFound("That venue does not exist.");
        }

        return Ok(venue);
    }

    /// <summary>
    /// Adds a new venue that organisers can then hold events at.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VenueDto>> Create(CreateVenueRequest request)
    {
        // Two venues with the same name in the same city would make the
        // dropdown impossible to use, so block it up front.
        var duplicate = await _context.Venues.AnyAsync(v =>
            v.VenueName == request.VenueName && v.City == request.City);

        if (duplicate)
        {
            return Conflict("A venue with that name already exists in that city.");
        }

        var venue = new Venue
        {
            VenueName = request.VenueName.Trim(),
            AddressLine1 = request.AddressLine1?.Trim(),
            City = request.City?.Trim(),
            Province = request.Province?.Trim(),
            Capacity = request.Capacity
        };

        _context.Venues.Add(venue);
        await _context.SaveChangesAsync();

        var dto = new VenueDto
        {
            VenueId = venue.VenueId,
            VenueName = venue.VenueName,
            AddressLine1 = venue.AddressLine1,
            City = venue.City,
            Province = venue.Province,
            Capacity = venue.Capacity,
            EventCount = 0
        };

        return CreatedAtAction(nameof(GetById), new { id = venue.VenueId }, dto);
    }

    /// <summary>
    /// Updates the details of an existing venue.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CreateVenueRequest request)
    {
        var venue = await _context.Venues.FindAsync(id);

        if (venue is null)
        {
            return NotFound("That venue does not exist.");
        }

        // Same duplicate rule as Create, except we skip the row we are
        // busy editing so it does not clash with itself.
        var clash = await _context.Venues.AnyAsync(v =>
            v.VenueId != id &&
            v.VenueName == request.VenueName &&
            v.City == request.City);

        if (clash)
        {
            return Conflict("Another venue already uses that name in that city.");
        }

        venue.VenueName = request.VenueName.Trim();
        venue.AddressLine1 = request.AddressLine1?.Trim();
        venue.City = request.City?.Trim();
        venue.Province = request.Province?.Trim();
        venue.Capacity = request.Capacity;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Removes a venue, provided no events are relying on it.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var venue = await _context.Venues
            .Include(v => v.Events)
            .FirstOrDefaultAsync(v => v.VenueId == id);

        if (venue is null)
        {
            return NotFound();
        }

        // Events have a required VenueId, so deleting a venue that is in
        // use would either orphan those rows or throw a foreign key error
        // from SQL Server. Far better to explain the problem ourselves.
        if (venue.Events.Any())
        {
            return BadRequest($"This venue is used by {venue.Events.Count} event(s) and cannot be deleted.");
        }

        _context.Venues.Remove(venue);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}