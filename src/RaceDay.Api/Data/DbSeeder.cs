using RaceDay.Api.Models;
using RaceDay.Api.Services; // part 3 

namespace RaceDay.Api.Data;

public static class DbSeeder
{
    // Only runs when the tables are empty, so restarting the API
    // will not keep adding duplicate rows
    public static void Seed(RaceDayDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        // Every seeded account uses the same demo password so that anyone marking
        // this project can sign in straight after cloning it. Hashed here rather
        // than hardcoded, otherwise PasswordHasher.Verify would never match.
        var demoPassword = PasswordHasher.Hash("Password123!");

        var organiser = new User
        {
            FirstName = "Thandi",
            LastName = "Mokoena",
            Email = "thandi.mokoena@racedaysa.co.za",
            PasswordHash = demoPassword, // part 3 - shared demo password
            // PasswordHash = "SEEDED_PLACEHOLDER_HASH", // part 2 
            PhoneNumber = "0823456789",
            DateOfBirth = new DateOnly(1988, 4, 17),
            Role = "Organiser"
        };

        var participant = new User
        {
            FirstName = "Sipho",
            LastName = "Naidoo",
            Email = "sipho.naidoo@example.com",
            PasswordHash = demoPassword, // part 3 - shared demo password
            // PasswordHash = "SEEDED_PLACEHOLDER_HASH", // part 2
            PhoneNumber = "0719876543",
            DateOfBirth = new DateOnly(1996, 11, 2),
            Role = "Participant"
        };

        var venue = new Venue
        {
            VenueName = "Zwartkops Raceway",
            AddressLine1 = "R55 Old Johannesburg Road",
            City = "Centurion",
            Province = "Gauteng",
            Capacity = 5000
        };

        var raceEvent = new Event
        {
            EventName = "Zwartkops Summer Series",
            Description = "A road race held on a closed circuit, open to runners and walkers.",
            EventDate = new DateOnly(2026, 2, 14),
            DistanceKm = 21.10m,
            EventType = "Run",
            Venue = venue,
            Organiser = organiser,
            Status = "Published"
        };

        var halfMarathon = new Category
        {
            Event = raceEvent,
            CategoryName = "Half Marathon",
            DistanceKm = 21.10m,
            EntryFee = 250.00m,
            MaxParticipants = 800
        };

        var funRun = new Category
        {
            Event = raceEvent,
            CategoryName = "Fun Run",
            DistanceKm = 5.00m,
            EntryFee = 80.00m,
            MaxParticipants = 1200
        };

        var enrolment = new Enrolment
        {
            Category = funRun,
            Participant = participant,
            Status = "Confirmed"
        };

        context.AddRange(organiser, participant, venue, raceEvent,
                         halfMarathon, funRun, enrolment);
        context.SaveChanges();
    }
}
