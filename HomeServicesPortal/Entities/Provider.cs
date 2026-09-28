namespace HomeServicesPortal.Entities;

public class Provider
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    /// <summary>Denormalized from UsersLogin; unique alternate key used by ProviderDocuments.</summary>
    public string MobileNo { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Cnic { get; set; } = string.Empty;

    public string? Gender { get; set; }

    public string? City { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Description { get; set; }

    public bool IsVerified { get; set; }

    public decimal AverageRating { get; set; }

    public int TotalReviews { get; set; }

    public int TotalJobsCompleted { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string? AvailableTiming { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? LocationUpdatedOn { get; set; }

    public DateTime CreatedOn { get; set; }

    /// <summary>DEPRECATED: mirrors the IsPrimary=1 row in ProviderCategories. Do not add new
    /// dependencies on this column — use ProviderCategories (via ProviderCategoryService) for
    /// category matching/membership. Scheduled for removal in a fast-follow cleanup.</summary>
    public int CategoryUid { get; set; }

    public UsersLogin User { get; set; } = null!;

    public ServiceCategory Category { get; set; } = null!;

    public ICollection<ProviderCategory> ProviderCategories { get; set; } = new List<ProviderCategory>();
}
