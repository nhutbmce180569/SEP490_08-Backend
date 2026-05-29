using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ContentAPI.Models;

public partial class StayHubContentDbContext : DbContext
{
    public StayHubContentDbContext()
    {
    }

    public StayHubContentDbContext(DbContextOptions<StayHubContentDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Banner> Banners { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<TicketType> TicketTypes { get; set; }

    public virtual DbSet<TourismInformation> TourismInformations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Banners__3214EC07B4CB6B89");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.Title).HasMaxLength(255);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC076FDE3FE9");

            entity.HasIndex(e => e.Slug, "UQ__Categori__BC7B5FB6A180FF0F").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Slug)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TicketType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TicketTy__3214EC07160337BF");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<TourismInformation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourismI__3214EC07C376ED0B");

            entity.ToTable("TourismInformation");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("Vietnam");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.SourceName).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
