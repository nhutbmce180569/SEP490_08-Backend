using Microsoft.EntityFrameworkCore;

namespace VoucherAPI.Models;

public partial class StayHubVoucherDbContext : DbContext
{
    public StayHubVoucherDbContext()
    {
    }

    public StayHubVoucherDbContext(DbContextOptions<StayHubVoucherDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    public virtual DbSet<UserVoucher> UserVouchers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Code).IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.DiscountType)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.Property(e => e.UsedCount).HasDefaultValue(0);

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<UserVoucher>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Available");

            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Voucher)
                .WithMany(p => p.UserVouchers)
                .HasForeignKey(d => d.VoucherId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
