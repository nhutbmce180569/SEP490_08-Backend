using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SystemAPI.Models;

public partial class StayHubSystemDbContext : DbContext
{
    public StayHubSystemDbContext()
    {
    }

    public StayHubSystemDbContext(DbContextOptions<StayHubSystemDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07872EEC31");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SystemSe__3214EC07801077B3");

            entity.HasIndex(e => e.SettingKey, "UQ__SystemSe__01E719AD3835A6CB").IsUnique();

            entity.Property(e => e.SettingKey)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
