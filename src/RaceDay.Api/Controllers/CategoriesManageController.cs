using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Contracts.Categories;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/Categories")]
public class CategoriesManageController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesManageController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Updates an existing category. Only the organiser who owns the
    /// parent event may change it.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] UpdateCategoryRequest request)
    {
        // No AsNoTracking here. EF Core must track the entity so that SaveChangesAsync can work out which columns actually changed.
        // Event is included because ownership lives on the parent, not on the category itself.
        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
        {
            return NotFound(new { message = $"No category was found with ID {id}." });
        }

        // TODO (Auth step): read the organiser from the session instead of
        // the request body, so a caller cannot simply claim to be someone else.
        if (category.Event is null || category.Event.OrganiserId != request.OrganiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only the organiser who owns this event may update its categories." });
        }

        // Business rule: you may not shrink the cap below the number of
        // people already entered, or you would have more entrants than places.
        var enrolledCount = await _context.Enrolments
            .CountAsync(e => e.CategoryId == id);

        if (request.MaxParticipants < enrolledCount)
        {
            ModelState.AddModelError(nameof(request.MaxParticipants),
                $"This category already has {enrolledCount} entrants, " +
                $"so the maximum cannot be lowered to {request.MaxParticipants}.");
            return ValidationProblem(ModelState);
        }

        // Overwrite the tracked entity. EventId is deliberately not touched:
        // a category must never be moved to a different event.
        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;

        await _context.SaveChangesAsync();

        // 204 is the conventional REST answer to a successful update:
        // it worked, and there is nothing new for the client to read.
        return NoContent();
    }

    /// <summary>
    /// Deletes a category. Only the owning organiser may remove it, and category that already has entrants may not be deleted.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(
        int id,
        [FromQuery] int organiserId)
    {
        // Enrolments are included so we can check for entrants before removing.
        var category = await _context.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category is null)
        {
            return NotFound(new { message = $"No category was found with ID {id}." });
        }

        // A DELETE request conventionally carries no body, so the organiser id arrives as a query parameter instead of in a DTO.
        if (category.Event is null || category.Event.OrganiserId != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only the organiser who owns this event may delete its categories." });
        }

        if (category.Enrolments.Count > 0)
        {
            return Conflict(new
            {
                message = "This category has participants enrolled and cannot be deleted."
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
