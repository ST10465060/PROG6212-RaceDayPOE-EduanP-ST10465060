using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Users;

// A user may change their name and contact details, but not their email, role or password through this endpoint. 
// Email is the login identifier and role decides permissions, so neither should be editable on a profile form.
public class UpdateProfileRequest
{
    [Required, MaxLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20), Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    public DateOnly DateOfBirth { get; set; }
}