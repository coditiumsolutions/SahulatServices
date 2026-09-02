using System;
using System.Collections.Generic;
using HomeServicesPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Data;

public partial class SahulatAppDbContext : DbContext
{
    public SahulatAppDbContext(DbContextOptions<SahulatAppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<ProviderAvailability> ProviderAvailabilities { get; set; }

    public virtual DbSet<ProviderDocument> ProviderDocuments { get; set; }

    public virtual DbSet<ProviderLocation> ProviderLocations { get; set; }

    public virtual DbSet<ProviderQuote> ProviderQuotes { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServiceRequest> ServiceRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Customer__C5B196027989B8CD");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.MobileNo)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Payments__C5B196020F9697C2");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BookingUid).HasColumnName("BookingUID");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TransactionNo)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ProviderAvailability>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Provider__C5B19602F9B16356");

            entity.ToTable("ProviderAvailability");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.IsOnline).HasDefaultValue(false);
            entity.Property(e => e.ProviderUid).HasColumnName("ProviderUID");

            entity.HasOne(d => d.ProviderU).WithMany(p => p.ProviderAvailabilities)
                .HasForeignKey(d => d.ProviderUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProviderA__Provi__47DBAE45");
        });

        modelBuilder.Entity<ProviderDocument>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Provider__C5B19602F27F76C2");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.DocumentNo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DocumentType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.ProviderUid).HasColumnName("ProviderUID");

            entity.HasOne(d => d.ProviderU).WithMany(p => p.ProviderDocuments)
                .HasForeignKey(d => d.ProviderUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProviderD__Provi__4AB81AF0");
        });

        modelBuilder.Entity<ProviderLocation>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Provider__C5B19602C850575D");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.ProviderUid).HasColumnName("ProviderUID");

            entity.HasOne(d => d.ProviderU).WithMany(p => p.ProviderLocations)
                .HasForeignKey(d => d.ProviderUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProviderL__Provi__440B1D61");
        });

        modelBuilder.Entity<ProviderQuote>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Provider__C5B196026063A0BE");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.DistanceKm)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("DistanceKM");
            entity.Property(e => e.ProviderUid).HasColumnName("ProviderUID");
            entity.Property(e => e.QuoteAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.QuoteDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Remarks)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RequestUid).HasColumnName("RequestUID");

            entity.HasOne(d => d.ProviderU).WithMany(p => p.ProviderQuotes)
                .HasForeignKey(d => d.ProviderUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProviderQ__Provi__5812160E");

            entity.HasOne(d => d.RequestU).WithMany(p => p.ProviderQuotes)
                .HasForeignKey(d => d.RequestUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProviderQ__Reque__571DF1D5");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__Reviews__C5B19602862AAE02");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.BookingUid).HasColumnName("BookingUID");
            entity.Property(e => e.CustomerUid).HasColumnName("CustomerUID");
            entity.Property(e => e.ProviderUid).HasColumnName("ProviderUID");
            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReviewText)
                .HasMaxLength(1000)
                .IsUnicode(false);

            entity.HasOne(d => d.CustomerU).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.CustomerUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__Custome__6A30C649");

            entity.HasOne(d => d.ProviderU).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.ProviderUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reviews__Provide__6B24EA82");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__ServiceC__C5B196023519CB75");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedOn)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(e => e.Uid).HasName("PK__ServiceR__C5B19602DA3F9164");

            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.CategoryUid).HasColumnName("CategoryUID");
            entity.Property(e => e.CustomerUid).HasColumnName("CustomerUID");
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.ProblemDescription).IsUnicode(false);
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ServiceAddress)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.CategoryU).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.CategoryUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceRe__Categ__534D60F1");

            entity.HasOne(d => d.CustomerU).WithMany(p => p.ServiceRequests)
                .HasForeignKey(d => d.CustomerUid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ServiceRe__Custo__52593CB8");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
