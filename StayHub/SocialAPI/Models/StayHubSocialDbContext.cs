using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SocialAPI.Models;

public partial class StayHubSocialDbContext : DbContext
{
    public StayHubSocialDbContext()
    {
    }

    public StayHubSocialDbContext(DbContextOptions<StayHubSocialDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChatMember> ChatMembers { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatRoom> ChatRooms { get; set; }

    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<LocationLog> LocationLogs { get; set; }

    public virtual DbSet<MomentComment> MomentComments { get; set; }

    public virtual DbSet<MomentReaction> MomentReactions { get; set; }

    public virtual DbSet<TourMoment> TourMoments { get; set; }

    public virtual DbSet<ContentReport> ContentReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChatMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMemb__3214EC074AF85742");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ChatRoom).WithMany(p => p.ChatMembers)
                .HasForeignKey(d => d.ChatRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChatMembe__ChatR__3F466844");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMess__3214EC076AE99A27");

            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.SentAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.ChatRoom).WithMany(p => p.ChatMessages)
                .HasForeignKey(d => d.ChatRoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChatMessa__ChatR__4316F928");
        });

        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatRoom__3214EC07CE58D007");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.IsGroupChat).HasDefaultValue(false);
            entity.Property(e => e.RoomName).HasMaxLength(255);
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Friendsh__3214EC07723C214A");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
        });

        modelBuilder.Entity<LocationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Location__3214EC07A2349584");

            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<MomentComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MomentCo__3214EC07692EA7BF");

            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getdate())");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Approved");

            entity.HasOne(d => d.Moment).WithMany(p => p.MomentComments)
                .HasForeignKey(d => d.MomentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MomentCom__Momen__4E88ABD4");
        });

        modelBuilder.Entity<MomentReaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MomentRe__3214EC07B56A0AA2");

            entity.Property(e => e.IsLike).HasDefaultValue(true);

            entity.HasOne(d => d.Moment).WithMany(p => p.MomentReactions)
                .HasForeignKey(d => d.MomentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MomentRea__Momen__4AB81AF0");
        });

        modelBuilder.Entity<TourMoment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TourMome__3214EC07889CED46");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Privacy)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Public");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Approved");
        });

        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContentType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Reason).HasMaxLength(255);
            entity.Property(e => e.Details).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
