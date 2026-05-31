using Microsoft.EntityFrameworkCore;

namespace AIAPI.Models;

public class StayHubAiDbContext : DbContext
{
    public StayHubAiDbContext(DbContextOptions<StayHubAiDbContext> options) : base(options)
    {
    }

    public DbSet<UserTourInteraction> UserTourInteractions => Set<UserTourInteraction>();
    public DbSet<ModelTrainingRun> ModelTrainingRuns => Set<ModelTrainingRun>();
    public DbSet<TourRelevanceJudgment> TourRelevanceJudgments => Set<TourRelevanceJudgment>();
    public DbSet<UserStudyAssignment> UserStudyAssignments => Set<UserStudyAssignment>();
    public DbSet<UserStudyResponse> UserStudyResponses => Set<UserStudyResponse>();

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

        modelBuilder.Entity<TourRelevanceJudgment>(entity =>
        {
            entity.HasIndex(e => new { e.ProfileSignature, e.TourId });
            entity.Property(e => e.ProfileSignature).HasMaxLength(32);
            entity.Property(e => e.ProfileQueryKey).HasMaxLength(128);
            entity.Property(e => e.Source).HasMaxLength(30);
            entity.Property(e => e.JudgeId).HasMaxLength(64);
        });

        modelBuilder.Entity<UserStudyAssignment>(entity =>
        {
            entity.HasIndex(e => new { e.SessionId, e.ScenarioId }).IsUnique();
            entity.Property(e => e.SessionId).HasMaxLength(64);
            entity.Property(e => e.StrategyForListA).HasMaxLength(32);
            entity.Property(e => e.StrategyForListB).HasMaxLength(32);
            entity.Property(e => e.ComparisonPair).HasMaxLength(64);
        });

        modelBuilder.Entity<UserStudyResponse>(entity =>
        {
            entity.HasIndex(e => new { e.SessionId, e.ScenarioId }).IsUnique();
            entity.Property(e => e.SessionId).HasMaxLength(64);
            entity.Property(e => e.PreferredList).HasMaxLength(8);
            entity.Property(e => e.AgeGroup).HasMaxLength(20);
            entity.Property(e => e.TravelExperience).HasMaxLength(30);
            entity.Property(e => e.OpenComment).HasMaxLength(500);
            entity.Property(e => e.ResponseSource).HasMaxLength(32);
            entity.HasOne(e => e.Assignment)
                .WithMany()
                .HasForeignKey(e => e.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
