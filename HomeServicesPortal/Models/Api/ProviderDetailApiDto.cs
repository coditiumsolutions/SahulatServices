namespace HomeServicesPortal.Models.Api;

public class ProviderDetailApiDto
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string MobileNo { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Cnic { get; set; } = string.Empty;

    public string? Gender { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Description { get; set; }

    public bool IsVerified { get; set; }

    public decimal AverageRating { get; set; }

    public int TotalReviews { get; set; }

    public int TotalJobsCompleted { get; set; }

    public bool IsAvailable { get; set; }

    public string? AvailableTiming { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }
}
