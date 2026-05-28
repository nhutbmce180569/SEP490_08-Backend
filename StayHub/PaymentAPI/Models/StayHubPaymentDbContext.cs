using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PaymentAPI.Models;

public partial class StayHubPaymentDbContext : DbContext
{
    public StayHubPaymentDbContext()
    {
    }

    public StayHubPaymentDbContext(DbContextOptions<StayHubPaymentDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transact__3214EC0729B38586");

            entity.Property(e => e.Provider)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ProviderTxnId)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
