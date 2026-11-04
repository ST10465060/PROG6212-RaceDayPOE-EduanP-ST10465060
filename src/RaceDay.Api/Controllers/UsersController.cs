using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;
using RaceDay.Contracts.Users;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public UsersController(RaceDayDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    /// <summary>
    /// Returns a single user's profile.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetUser(int id)
    {
        // Projected straight into the DTO so the password hash never even leaves the database, let alone the API.
        var user = await _context.Users
            .Where(u => u.UserId == id)
            .Select(u => new UserProfileDto
            {
                UserId = u.UserId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                DateOfBirth = u.DateOfBirth,
                Role = u.Role,
                ProfilePictureUrl = u.ProfilePictureUrl,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound(new { message = $"No user was found with ID {id}." });
        }

        return Ok(user);
    }

    /// <summary>
    /// Updates the signed in user's own profile details.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(int id, UpdateProfileRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound(new { message = $"No user was found with ID {id}." });
        }

        // Same age rule as registration, otherwise someone could register legitimately and then edit their birth date to something invalid.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.DateOfBirth >= today)
        {
            return BadRequest(new { message = "The date of birth must be in the past." });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.DateOfBirth = request.DateOfBirth;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Uploads or replaces the user's profile picture.
    /// </summary>
    [HttpPost("{id:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProfilePicture(int id, IFormFile file)
    {
        var user = await _context.Users.FindAsync(id);

        if (user is null)
        {
            return NotFound(new { message = $"No user was found with ID {id}." });
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowed.Contains(extension))
        {
            return BadRequest(new { message = "Only JPG, PNG or WEBP images are allowed." });
        }

        // Profile pictures are small, so a 2 MB cap is enough.
        const long maxBytes = 2 * 1024 * 1024;
        if (file.Length > maxBytes)
        {
            return BadRequest(new { message = "The image may not be larger than 2 MB." });
        }

        // Clear out the previous picture so replaced images do not linger.
        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            await _blobStorage.DeleteAsync(user.ProfilePictureUrl);
        }

        await using var stream = file.OpenReadStream();
        var url = await _blobStorage.UploadAsync(stream, file.FileName, file.ContentType);

        user.ProfilePictureUrl = url;
        await _context.SaveChangesAsync();

        return Ok(new { profilePictureUrl = url });
    }
}
