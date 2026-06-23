using HomeServicesPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Data;

public partial class SahulatAppDbContext
{
    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }

    public virtual DbSet<ProviderProfile> ProviderProfiles { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.ToTable("Users");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MobileNo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.UserType)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.DefaultAddress)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.ProfileImage)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.UserUid).HasColumnName("UserUID");

            entity.HasOne(d => d.UserU).WithOne(p => p.CustomerProfile)
                .HasForeignKey<CustomerProfile>(d => d.UserUid)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProviderProfile>(entity =>
        {
            entity.HasKey(e => e.Uid);

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.CategoryUid).HasColumnName("CategoryUID");
            entity.Property(e => e.Cnic)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CNIC");
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.Rating).HasColumnType("decimal(3, 2)");
            entity.Property(e => e.UserUid).HasColumnName("UserUID");

            entity.HasOne(d => d.UserU).WithOne(p => p.ProviderProfile)
                .HasForeignKey<ProviderProfile>(d => d.UserUid)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
