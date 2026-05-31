using Microsoft.EntityFrameworkCore;

namespace AIAPI.Models;

public class StayHubAiDbContext : DbContext
{
    public StayHubAiDbContext(DbContextOptions<StayHubAiDbContext> options) : base(options)
    {
    }

    public DbSet<UserTourInteraction> UserTourInteractions => Set<UserTourInteraction>();
    public DbSet<ModelTrainingRun> ModelTrainingRuns => Set<ModelTrainingRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserTourInteraction>(entity =>
        {
            entity.HasIndex(e => new { e.CustomerId, e.TourId, e.InteractionType });
            entity.Property(e => e.InteractionType).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(64);
        });

        modelBuilder.Entity<ModelTrainingRun>(entity =>
        {
            entity.Property(e => e.ModelName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(30);
        });
    }
}
