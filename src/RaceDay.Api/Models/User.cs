using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }

    [Required, MaxLength(60)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(120), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateOnly DateOfBirth { get; set; }

    // Either "Organiser" or "Participant"
    [Required, MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // An organiser owns many events, a participant has many enrolments
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}