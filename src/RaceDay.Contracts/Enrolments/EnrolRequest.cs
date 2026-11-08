using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Enrolments;

// What a participant sends when they sign up for a race category.
// There is no EnrolmentId and no status, the API decides both of those.
public class EnrolRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    public int ParticipantId { get; set; }
}
