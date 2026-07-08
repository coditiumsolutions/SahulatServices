namespace HomeServicesPortal.Models.Api;

/// <summary>Providers table fields for API responses.</summary>
public class ProviderProfileApiDto
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string? FullName { get; set; }

    public int? CategoryUid { get; set; }

    public string? Cnic { get; set; }

    public int? ExperienceYears { get; set; }

    public decimal? Rating { get; set; }

    public bool? IsVerified { get; set; }
}
