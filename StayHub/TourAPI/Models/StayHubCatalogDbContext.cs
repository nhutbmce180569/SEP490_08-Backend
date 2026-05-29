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

    public virtual DbSet<TourScheduleTicket> TourScheduleTickets { get; set; }

    public virtual DbSet<Wishlist> Wishlists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC0726437281");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Tour).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__TourId__4E88ABD4");
        });

        modelBuilder.Entity<ReviewReply>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReviewRe__3214EC071321BE16");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewReplies)
                .HasForeignKey(d => d.ReviewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReviewRep__Revie__534D60F1");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tours__3214EC077EB37361");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TourItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourItin__3214EC07CE56278A");

            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Tour).WithMany(p => p.TourItineraries)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourItine__TourI__3B75D760");
        });

        modelBuilder.Entity<TourSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07C67EF453");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourSchedules)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__TourI__3E52440B");
        });

        modelBuilder.Entity<TourScheduleItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC078D631705");

            entity.Property(e => e.ItineraryDate).HasColumnType("datetime");
            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleItineraries)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__412EB0B6");
        });

        modelBuilder.Entity<TourScheduleStaff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07A205F1E0");

            entity.Property(e => e.AssignedRole).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleStaffs)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__440B1D61");
        });

        modelBuilder.Entity<TourScheduleTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07E70CF3E4");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SoldQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleTickets)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__48CFD27E");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Wishlist__3214EC0773FBE4C4");

            entity.HasOne(d => d.Tour).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wishlists__TourI__4BAC3F29");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
