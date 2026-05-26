using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TourAPI.Models;

public partial class StayHubCatalogDbContext : DbContext
{
    public StayHubCatalogDbContext()
    {
    }

    public StayHubCatalogDbContext(DbContextOptions<StayHubCatalogDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ReviewReply> ReviewReplies { get; set; }

    public virtual DbSet<Tour> Tours { get; set; }

    public virtual DbSet<TourItinerary> TourItineraries { get; set; }

    public virtual DbSet<TourSchedule> TourSchedules { get; set; }

    public virtual DbSet<TourScheduleItinerary> TourScheduleItineraries { get; set; }

    public virtual DbSet<TourScheduleStaff> TourScheduleStaffs { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC0722144CCC");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Tour).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__TourId__48CFD27E");
        });

        modelBuilder.Entity<ReviewReply>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReviewRe__3214EC07AC7BE0E4");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewReplies)
                .HasForeignKey(d => d.ReviewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReviewRep__Revie__4D94879B");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tours__3214EC0793C4F950");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Active");
        });

        modelBuilder.Entity<TourItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourItin__3214EC070CDAD78C");

            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Tour).WithMany(p => p.TourItineraries)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourItine__TourI__3A81B327");
        });

        modelBuilder.Entity<TourSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07891A2DE7");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourSchedules)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__TourI__3D5E1FD2");
        });

        modelBuilder.Entity<TourScheduleItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07F0F71901");

            entity.Property(e => e.ItineraryDate).HasColumnType("datetime");
            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleItineraries)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__403A8C7D");
        });

        modelBuilder.Entity<TourScheduleStaff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07E8BD784C");

            entity.Property(e => e.AssignedRole).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleStaffs)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__4316F928");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Wishlist__3214EC074608718B");

            entity.HasOne(d => d.Tour).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wishlists__TourI__45F365D3");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
