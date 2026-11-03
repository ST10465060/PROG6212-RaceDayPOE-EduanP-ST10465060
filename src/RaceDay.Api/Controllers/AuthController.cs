using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts.Auth;
using System;
using System.Threading.Tasks;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public AuthController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new organiser or participant account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthUserDto>> Register(RegisterRequest request)
    {
        // Emails are the login identifier, so they have to be unique. Compared
        // in lower case because Beth@x.com and beth@x.com are the same person.
        var email = request.Email.Trim().ToLowerInvariant();

        var emailTaken = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (emailTaken)
        {
            return Conflict(new { message = "An account with that email address already exists." });
        }

        // Business rule: entrants must be at least 16 on the day they sign up.
        var age = CalculateAge(request.DateOfBirth);
        if (age < 16)
        {
            return BadRequest(new { message = "You must be at least 16 years old to register." });
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = ToDto(user);

        // 201 Created, with a Location header pointing at the new resource.
        return CreatedAtAction(nameof(Register), new { id = user.UserId }, dto);
    }

    /// <summary>
    /// Checks an email and password pair and returns the user's details.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthUserDto>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        // Note the identical message for both failures. Saying "no such email" would let someone probe the system to discover who has an account.
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "The email address or password is incorrect." });
        }

        return Ok(ToDto(user));
    }

    // Shared mapping so register and login always return the same shape.
    private static AuthUserDto ToDto(User user) => new()
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        ProfilePictureUrl = user.ProfilePictureUrl
    };

    // Works out a whole age in years, accounting for whether the birthday has already happened this year.
    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;

        if (dateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
