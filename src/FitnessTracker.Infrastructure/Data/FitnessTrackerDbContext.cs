using FitnessTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FitnessTracker.Infrastructure.Data;

public class FitnessTrackerDbContext : DbContext
{
    public FitnessTrackerDbContext(DbContextOptions<FitnessTrackerDbContext> options) : base(options) { }

    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Threshold> Thresholds => Set<Threshold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Athlete>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Activity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Athlete).WithMany(ath => ath.Activities).HasForeignKey(a => a.AthleteId);

            // Speeds up the "has this file already been imported" duplicate check
            // and the dashboard's date-range + sport filter queries.
            e.HasIndex(a => new { a.AthleteId, a.StartTimeUtc, a.Sport });

            e.Property(a => a.SourceFileName).HasMaxLength(500);
        });

        modelBuilder.Entity<Threshold>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasOne(t => t.Athlete).WithMany(a => a.Thresholds).HasForeignKey(t => t.AthleteId);
            e.HasIndex(t => new { t.AthleteId, t.Sport, t.EffectiveDate });
            e.Property(t => t.Source).HasMaxLength(200);
        });
    }
}
