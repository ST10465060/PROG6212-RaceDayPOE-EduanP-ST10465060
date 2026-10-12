using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // No two accounts may share an email address
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Decimal precision, otherwise EF warns and SQL Server guesses
        modelBuilder.Entity<Event>()
            .Property(e => e.DistanceKm)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Category>()
            .Property(c => c.DistanceKm)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Category>()
            .Property(c => c.EntryFee)
            .HasPrecision(8, 2);

        // A category name may only appear once within the same event
        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.EventId, c.CategoryName })
            .IsUnique();

        // A participant may only enter a given category once
        modelBuilder.Entity<Enrolment>()
            .HasIndex(e => new { e.CategoryId, e.ParticipantId })
            .IsUnique();

        // One result per enrolment
        modelBuilder.Entity<Result>()
            .HasIndex(r => r.EnrolmentId)
            .IsUnique();

        // An event belongs to one organiser, who is a User
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Venue)
            .WithMany(v => v.Events)
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Removing an event removes the categories that belong to it
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}