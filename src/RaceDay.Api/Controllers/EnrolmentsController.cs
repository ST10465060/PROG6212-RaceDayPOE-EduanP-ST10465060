using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts.Enrolments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enrols a participant into a race category.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MyEnrolmentDto>> Enrol(EnrolRequest request)
    {
        // Pull the category together with its event, because we need the
        // event date and the capacity rules before we allow the entry.
        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);

        if (category is null)
        {
            return NotFound("That race category does not exist.");
        }

        var participant = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.ParticipantId);

        if (participant is null)
        {
            return NotFound("That participant account does not exist.");
        }

        // Organisers run races, they do not enter them. Blocking this here
        // means the MVC app cannot accidentally create a nonsense entry.
        if (participant.Role != "Participant")
        {
            return BadRequest("Only participant accounts can enrol in a category.");
        }

        // An event still sitting in Draft is not open to the public yet.
        if (category.Event!.Status != "Published")
        {
            return BadRequest("This event is not open for entries yet.");
        }

        // No entering a race that has already been run.
        if (category.Event.EventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest("This event has already taken place.");
        }

        // Stop the same person entering the same category twice.
        var alreadyIn = await _context.Enrolments.AnyAsync(e =>
            e.CategoryId == request.CategoryId &&
            e.ParticipantId == request.ParticipantId);

        if (alreadyIn)
        {
            return Conflict("You are already entered in this category.");
        }

        // Capacity check. Count what is there now and compare to the limit
        // the organiser set when they created the category.
        var taken = await _context.Enrolments
            .CountAsync(e => e.CategoryId == request.CategoryId);

        if (taken >= category.MaxParticipants)
        {
            return Conflict("This category is full.");
        }

        var enrolment = new Enrolment
        {
            CategoryId = request.CategoryId,
            ParticipantId = request.ParticipantId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Confirmed"
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var dto = new MyEnrolmentDto
        {
            EnrolmentId = enrolment.EnrolmentId,
            EventId = category.EventId,
            EventName = category.Event.EventName,
            EventDate = category.Event.EventDate,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            Status = enrolment.Status,
            EnrolmentDate = enrolment.EnrolmentDate
        };

        return CreatedAtAction(nameof(GetMyEnrolments),
            new { participantId = request.ParticipantId }, dto);
    }

    /// <summary>
    /// Every race a given participant has entered, newest first.
    /// </summary>
    [HttpGet("participant/{participantId:int}")]
    public async Task<ActionResult<IEnumerable<MyEnrolmentDto>>> GetMyEnrolments(int participantId)
    {
        // Note the double hop: an Enrolment belongs to a Category, and the
        // Category is the thing that knows which Event it belongs to.
        var enrolments = await _context.Enrolments
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new MyEnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                EventId = e.Category!.EventId,
                EventName = e.Category.Event!.EventName,
                EventDate = e.Category.Event.EventDate,
                CategoryName = e.Category.CategoryName,
                DistanceKm = e.Category.DistanceKm,
                EntryFee = e.Category.EntryFee,
                Status = e.Status,
                EnrolmentDate = e.EnrolmentDate,

                // ToString on a TimeSpan is not something EF can translate
                // into SQL, so the result is shaped after the query runs.
                FinishTime = e.Result != null
                    ? e.Result.FinishTime.ToString()
                    : null,
                Position = e.Result != null ? e.Result.Position : null
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>
    /// The full entrant list for one event, for the organiser who owns it.
    /// </summary>
    [HttpGet("event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<EventEnrolmentDto>>> GetEventEnrolments(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(ev => ev.EventId == eventId);
        if (!eventExists)
        {
            return NotFound("That event does not exist.");
        }

        var entrants = await _context.Enrolments
            .Where(e => e.Category!.EventId == eventId)
            .OrderBy(e => e.Category!.CategoryName)
            .ThenBy(e => e.Participant!.LastName)
            .Select(e => new EventEnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName,
                Email = e.Participant.Email,
                CategoryName = e.Category!.CategoryName,
                Status = e.Status,
                EnrolmentDate = e.EnrolmentDate,
                HasResult = e.Result != null
            })
            .ToListAsync();

        return Ok(entrants);
    }

    /// <summary>
    /// Cancels an entry. We delete rather than flag it, since a withdrawn
    /// entrant should free up their place in the category straight away.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment is null)
        {
            return NotFound();
        }

        // Once a result is captured the entry is part of the race record,
        // so it stays put.
        if (enrolment.Result is not null)
        {
            return BadRequest("This entry already has a result and cannot be cancelled.");
        }

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}