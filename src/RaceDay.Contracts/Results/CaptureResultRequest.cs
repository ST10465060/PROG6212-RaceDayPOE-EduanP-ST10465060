using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts.Results;

// What the organiser submits after a race, one entrant at a time.
public class CaptureResultRequest
{
    [Required]
    public int EnrolmentId { get; set; }

    // Sent as a string such as "00:45:12" because that is how a TimeSpan
    // travels over JSON. Parsing it ourselves gives a far friendlier error
    // than the default model binder does.
    [Required(ErrorMessage = "A finish time is required, for example 00:45:12.")]
    public string FinishTime { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Position must be 1 or higher.")]
    public int Position { get; set; }
}