using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int ParticipantId { get; set; }
    public User? Participant { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Confirmed";

    public Result? Result { get; set; }
}