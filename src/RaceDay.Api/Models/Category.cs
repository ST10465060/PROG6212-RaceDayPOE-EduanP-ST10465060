using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    [Required, MaxLength(80)]
    public string CategoryName { get; set; } = string.Empty;

    // The specific distance a participant actually enters
    [Range(0.1, 999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, 100000)]
    public decimal EntryFee { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}