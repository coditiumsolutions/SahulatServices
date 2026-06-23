namespace HomeServicesPortal.Models.Api;

public class ServiceProviderApiDto
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? MobileNo { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    public decimal Rating { get; set; }

    public bool IsVerified { get; set; }

    public string? ProfileImageUrl { get; set; }

    public DateTime? CreatedOn { get; set; }
}
