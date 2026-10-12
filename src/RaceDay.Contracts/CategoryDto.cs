namespace RaceDay.Contracts.Events;

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public int EnrolledCount { get; set; }
    public int SpacesRemaining { get; set; }
}
