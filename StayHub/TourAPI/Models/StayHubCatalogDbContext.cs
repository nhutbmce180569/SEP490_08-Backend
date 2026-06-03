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
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC07925B9B06");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Tour).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__TourId__619B8048");
        });

        modelBuilder.Entity<ReviewReply>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReviewRe__3214EC073B808A91");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewReplies)
                .HasForeignKey(d => d.ReviewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReviewRep__Revie__6754599E");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tours__3214EC076B8BBCEA");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.SourceName).HasMaxLength(255);
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TourItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourItin__3214EC07EB6D4516");

            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Tour).WithMany(p => p.TourItineraries)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourItine__TourI__4E88ABD4");
        });

        modelBuilder.Entity<TourSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC079C29120F");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourSchedules)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__TourI__5165187F");
        });

        modelBuilder.Entity<TourScheduleItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC0763AFC147");

            entity.Property(e => e.ItineraryDate).HasColumnType("datetime");
            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleItineraries)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__5441852A");
        });

        modelBuilder.Entity<TourScheduleStaff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07F2417197");

            entity.Property(e => e.AssignedRole).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleStaffs)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__571DF1D5");
        });

        modelBuilder.Entity<TourScheduleTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC077DEAE26A");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SoldQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleTickets)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__5BE2A6F2");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Wishlist__3214EC0785EC83C6");

            entity.HasOne(d => d.Tour).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wishlists__TourI__5EBF139D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
