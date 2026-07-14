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

    public virtual DbSet<Promotion> Promotions { get; set; }

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
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Promotio__3214EC07556509CB");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscountType).HasMaxLength(50);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasMany(d => d.TourScheduleTickets).WithMany(p => p.Promotions)
                .UsingEntity<Dictionary<string, object>>(
                    "PromotionTicket",
                    r => r.HasOne<TourScheduleTicket>().WithMany()
                        .HasForeignKey("TourScheduleTicketId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Promotion__TourS__5070F446"),
                    l => l.HasOne<Promotion>().WithMany()
                        .HasForeignKey("PromotionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__Promotion__Promo__4F7CD00D"),
                    j =>
                    {
                        j.HasKey("PromotionId", "TourScheduleTicketId").HasName("PK__Promotio__990CB55382DAD1AF");
                        j.ToTable("PromotionTickets");
                    });
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reviews__3214EC07A9F3272F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Tour).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__TourId__571DF1D5");
        });

        modelBuilder.Entity<ReviewReply>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReviewRe__3214EC07A5D45219");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.Review).WithMany(p => p.ReviewReplies)
                .HasForeignKey(d => d.ReviewId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReviewRep__Revie__5CD6CB2B");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tours__3214EC07EB8F4649");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.SourceName)
                .HasMaxLength(255)
                .HasDefaultValue("Vietnam National Administration of Tourism");
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<TourItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourItin__3214EC078EA6091E");

            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Tour).WithMany(p => p.TourItineraries)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourItine__TourI__3C69FB99");
        });

        modelBuilder.Entity<TourSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07AFC55F9D");

            entity.HasOne(d => d.Tour).WithMany(p => p.TourSchedules)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__TourI__3F466844");
        });

        modelBuilder.Entity<TourScheduleItinerary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC077D5513E5");

            entity.Property(e => e.ItineraryDate).HasColumnType("datetime");
            entity.Property(e => e.LocationName).HasMaxLength(255);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleItineraries)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__4222D4EF");
        });

        modelBuilder.Entity<TourScheduleStaff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC075BA8C3E7");

            entity.Property(e => e.AssignedRole).HasMaxLength(255);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleStaffs)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__44FF419A");
        });

        modelBuilder.Entity<TourScheduleTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourSche__3214EC07A8DD21C7");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SoldQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Schedule).WithMany(p => p.TourScheduleTickets)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TourSched__Sched__49C3F6B7");
        });

        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Wishlist__3214EC07EA73861C");

            entity.HasIndex(e => new { e.CustomerId, e.TourId }, "UQ_Wishlists_Customer_Tour").IsUnique();

            entity.HasOne(d => d.Tour).WithMany(p => p.Wishlists)
                .HasForeignKey(d => d.TourId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wishlists__TourI__5441852A");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
