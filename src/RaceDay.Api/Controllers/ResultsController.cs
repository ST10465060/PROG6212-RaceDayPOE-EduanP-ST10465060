using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Captures a finish time and position for one entrant.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ResultDto>> Capture(CaptureResultRequest request)
    {
        // Parse the time first. No point touching the database if the
        // organiser typed something that is not a time at all.
        if (!TimeSpan.TryParse(request.FinishTime, out var finishTime))
        {
            return BadRequest("Finish time must look like 00:45:12 (hours:minutes:seconds).");
        }

        if (finishTime <= TimeSpan.Zero)
        {
            return BadRequest("Finish time must be greater than zero.");
        }

        // Load the whole chain in one query, since we need the event name
        // for the response and the event date to check the race has run.
        var enrolment = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
                .ThenInclude(c => c!.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

        if (enrolment is null)
        {
            return NotFound("That enrolment does not exist.");
        }

        // One result per entry. If they need to correct a mistake they use
        // the PUT below, not a second POST.
        if (enrolment.Result is not null)
        {
            return Conflict("This entrant already has a result. Use the update endpoint to change it.");
        }

        // A result before the race has happened would be nonsense data.
        if (enrolment.Category!.Event!.EventDate > DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest("You cannot capture results before the event has taken place.");
        }

        // Two people cannot finish in the same place in the same category.
        var positionTaken = await _context.Results.AnyAsync(r =>
            r.Enrolment!.CategoryId == enrolment.CategoryId &&
            r.Position == request.Position);

        if (positionTaken)
        {
            return Conflict($"Position {request.Position} has already been given to another entrant in this category.");
        }

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = finishTime,
            Position = request.Position,
            RecordedAt = DateTime.UtcNow
        };

        _context.Results.Add(result);

        // The entry is now finished rather than simply confirmed.
        enrolment.Status = "Completed";

        await _context.SaveChangesAsync();

        var dto = BuildDto(result, enrolment);

        return CreatedAtAction(nameof(GetByEnrolment),
            new { enrolmentId = result.EnrolmentId }, dto);
    }

    /// <summary>
    /// Corrects a result that was captured incorrectly.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CaptureResultRequest request)
    {
        if (!TimeSpan.TryParse(request.FinishTime, out var finishTime))
        {
            return BadRequest("Finish time must look like 00:45:12 (hours:minutes:seconds).");
        }

        var result = await _context.Results
            .Include(r => r.Enrolment)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null)
        {
            return NotFound();
        }

        // Same position rule as above, but this time we must ignore the
        // row we are editing, otherwise it clashes with itself.
        var clash = await _context.Results.AnyAsync(r =>
            r.ResultId != id &&
            r.Enrolment!.CategoryId == result.Enrolment!.CategoryId &&
            r.Position == request.Position);

        if (clash)
        {
            return Conflict($"Position {request.Position} is already taken in this category.");
        }

        result.FinishTime = finishTime;
        result.Position = request.Position;
        result.RecordedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// The result for one specific entry, if it has been captured yet.
    /// </summary>
    [HttpGet("enrolment/{enrolmentId:int}")]
    public async Task<ActionResult<ResultDto>> GetByEnrolment(int enrolmentId)
    {
        var result = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e!.Category)
                    .ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(r => r.EnrolmentId == enrolmentId);

        if (result is null)
        {
            return NotFound("No result has been captured for this entry yet.");
        }

        return Ok(BuildDto(result, result.Enrolment!));
    }

    /// <summary>
    /// The full results board for an event, ordered as it would be read out.
    /// </summary>
    [HttpGet("event/{eventId:int}")]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetByEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(ev => ev.EventId == eventId);
        if (!eventExists)
        {
            return NotFound("That event does not exist.");
        }

        var results = await _context.Results
            .Where(r => r.Enrolment!.Category!.EventId == eventId)
            .OrderBy(r => r.Enrolment!.Category!.CategoryName)
            .ThenBy(r => r.Position)
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                ParticipantId = r.Enrolment!.ParticipantId,
                ParticipantName = r.Enrolment.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                CategoryName = r.Enrolment.Category!.CategoryName,
                EventName = r.Enrolment.Category.Event!.EventName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                FinishTime = r.FinishTime.ToString(),
                Position = r.Position,
                RecordedAt = r.RecordedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>
    /// Every result a participant has ever earned, newest race first.
    /// </summary>
    [HttpGet("participant/{participantId:int}")]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetByParticipant(int participantId)
    {
        var results = await _context.Results
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .OrderByDescending(r => r.Enrolment!.Category!.Event!.EventDate)
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                ParticipantId = r.Enrolment!.ParticipantId,
                ParticipantName = r.Enrolment.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                CategoryName = r.Enrolment.Category!.CategoryName,
                EventName = r.Enrolment.Category.Event!.EventName,
                EventDate = r.Enrolment.Category.Event.EventDate,
                FinishTime = r.FinishTime.ToString(),
                Position = r.Position,
                RecordedAt = r.RecordedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    // Small helper so the two single-item endpoints build the response the
    // same way instead of repeating fifteen lines of mapping twice.
    private static ResultDto BuildDto(Result result, Enrolment enrolment)
    {
        return new ResultDto
        {
            ResultId = result.ResultId,
            EnrolmentId = result.EnrolmentId,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = $"{enrolment.Participant!.FirstName} {enrolment.Participant.LastName}",
            CategoryName = enrolment.Category!.CategoryName,
            EventName = enrolment.Category.Event!.EventName,
            EventDate = enrolment.Category.Event.EventDate,
            FinishTime = result.FinishTime.ToString(),
            Position = result.Position,
            RecordedAt = result.RecordedAt
        };
    }
}