namespace HomeServicesPortal.Entities;

public class Provider
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Cnic { get; set; } = string.Empty;

    public string? Gender { get; set; }

    public int? ExperienceYears { get; set; }

    public string? Description { get; set; }

    public bool IsVerified { get; set; }

    public decimal AverageRating { get; set; }

    public int TotalReviews { get; set; }

    public int TotalJobsCompleted { get; set; }

    public bool IsAvailable { get; set; } = true;

    public string? AvailableTiming { get; set; }

    public DateTime CreatedOn { get; set; }

    public int CategoryUid { get; set; }

    public UsersLogin User { get; set; } = null!;

    public ServiceCategory Category { get; set; } = null!;
}
