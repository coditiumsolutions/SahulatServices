using HomeServicesPortal.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Data;

/// <summary>EF Core context for UsersLogin and profile tables (maps to existing SQL Server schema).</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UsersLogin> UsersLogins => Set<UsersLogin>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<Staff> Staff => Set<Staff>();

    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();

    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();

    public DbSet<CustomerServiceRequest> CustomerServiceRequests => Set<CustomerServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsersLogin>(entity =>
        {
            entity.ToTable("UsersLogin");
            entity.HasKey(e => e.Uid);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.MobileNo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.UserType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.HasIndex(e => e.MobileNo).IsUnique();
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(e => e.Uid);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UserUid).HasColumnName("UserUID");
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Cnic).HasMaxLength(15).HasColumnName("CNIC");
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(e => e.User)
                .WithOne(u => u.Client)
                .HasForeignKey<Client>(e => e.UserUid)
                .HasConstraintName("FK_Clients_UsersLogin")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.ToTable("Providers");
            entity.HasKey(e => e.Uid);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UserUid).HasColumnName("UserUID");
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Cnic).HasMaxLength(15).HasColumnName("CNIC").IsRequired();
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.ExperienceYears).HasDefaultValue(0);
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3,2)").HasDefaultValue(0m);
            entity.Property(e => e.TotalReviews).HasDefaultValue(0);
            entity.Property(e => e.TotalJobsCompleted).HasDefaultValue(0);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.AvailableTiming)
                .HasMaxLength(100)
                .HasColumnName("AvailableTiming");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.CategoryUid).HasColumnName("CategoryUID");

            entity.HasOne(e => e.User)
                .WithOne(u => u.Provider)
                .HasForeignKey<Provider>(e => e.UserUid)
                .HasConstraintName("FK_Providers_UsersLogin")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Providers)
                .HasForeignKey(e => e.CategoryUid)
                .HasConstraintName("FK_Providers_ServiceCategories")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.ToTable("ServiceCategories");
            entity.HasKey(e => e.Uid);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.CategoryName).HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("Staff");
            entity.HasKey(e => e.Uid);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.UserUid).HasColumnName("UserUID");
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.IsAdmin).HasDefaultValue(false);
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(e => e.User)
                .WithOne(u => u.Staff)
                .HasForeignKey<Staff>(e => e.UserUid)
                .HasConstraintName("FK_Staff_UsersLogin")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClientAddress>(entity =>
        {
            entity.ToTable("ClientAddresses");
            entity.HasKey(e => e.Uid);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.ClientUid).HasColumnName("ClientUID");
            entity.Property(e => e.AddressTitle).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FullAddress).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Area).HasMaxLength(150).IsRequired();
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Latitude).HasColumnType("decimal(10,7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10,7)");

            entity.HasOne(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.ClientUid)
                .HasConstraintName("FK_ClientAddresses_Clients")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerServiceRequest>(entity =>
        {
            entity.ToTable("CustomerServiceRequests");
            entity.HasKey(e => e.Uid);
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.ClientUid).HasColumnName("ClientUID");
            entity.Property(e => e.CategoryUid).HasColumnName("CategoryUID");
            entity.Property(e => e.ClientAddressUid).HasColumnName("ClientAddressUID");
            entity.Property(e => e.ServiceTitle).HasMaxLength(150).IsRequired();
            entity.Property(e => e.ServiceDescription).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.PreferredServiceDate).HasColumnType("date");
            entity.Property(e => e.PreferredServiceTime).HasMaxLength(50);
            entity.Property(e => e.IsUrgent).HasDefaultValue(false);
            entity.Property(e => e.ContactPerson).HasMaxLength(150);
            entity.Property(e => e.ContactNo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EstimatedBudget).HasColumnType("decimal(12,2)");
            entity.Property(e => e.Status).HasMaxLength(30).IsRequired().HasDefaultValue("Pending");
            entity.Property(e => e.Remarks).HasMaxLength(500);
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(e => e.Client)
                .WithMany()
                .HasForeignKey(e => e.ClientUid)
                .HasConstraintName("FK_CustomerServiceRequests_Clients")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryUid)
                .HasConstraintName("FK_CustomerServiceRequests_ServiceCategories")
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ClientAddress)
                .WithMany(a => a.ServiceRequests)
                .HasForeignKey(e => e.ClientAddressUid)
                .HasConstraintName("FK_CustomerServiceRequests_ClientAddresses")
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
