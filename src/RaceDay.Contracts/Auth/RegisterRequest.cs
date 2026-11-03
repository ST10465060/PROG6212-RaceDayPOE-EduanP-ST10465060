using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Auth;

// What the registration form sends. 
// Note there is no UserId here, a client must never be allowed to choose its own primary key.
public class RegisterRequest
{
    [Required, MaxLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(120), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "The password must be at least 6 characters long.")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20), Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    public DateOnly DateOfBirth { get; set; }

    // Only these two roles exist in the system.
    [Required]
    [RegularExpression("Organiser|Participant",
        ErrorMessage = "Role must be either Organiser or Participant.")]
    public string Role { get; set; } = string.Empty;
}
