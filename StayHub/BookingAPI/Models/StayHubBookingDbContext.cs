using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BookingAPI.Models;

public partial class StayHubBookingDbContext : DbContext
{
    public StayHubBookingDbContext()
    {
    }

    public StayHubBookingDbContext(DbContextOptions<StayHubBookingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CancellationRequest> CancellationRequests { get; set; }


    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      

        modelBuilder.Entity<CancellationRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cancella__3214EC077BB98D25");

            entity.Property(e => e.AccountHolderName).HasMaxLength(255);
            entity.Property(e => e.AccountNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.BankName).HasMaxLength(255);
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Order).WithMany(p => p.CancellationRequests)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CancellationRequests_Orders");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Orders__3214EC07174C4F18");

            entity.HasIndex(e => e.InviteToken, "UQ__Orders__AB479560AAAFDD9F").IsUnique();

            entity.Property(e => e.DiscountValue).HasDefaultValue(0L);
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InviteToken)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.OrderedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3214EC07147B3A51");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderDetails_Orders");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Tickets__3214EC078955215E");

            entity.HasIndex(e => e.QrCode, "UQ__Tickets__EE32FA44AF8DBE9A").IsUnique();

            entity.Property(e => e.AttendeeName).HasMaxLength(255);
            entity.Property(e => e.CheckInStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Gender)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IdCard)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Nationality).HasMaxLength(100);
            entity.Property(e => e.QrCode)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.OrderDetail).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_OrderDetails");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
