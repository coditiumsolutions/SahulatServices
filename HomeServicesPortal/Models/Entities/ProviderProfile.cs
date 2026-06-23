namespace HomeServicesPortal.Models.Entities;

public partial class ProviderProfile
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public int? CategoryUid { get; set; }

    public string? Cnic { get; set; }

    public int? ExperienceYears { get; set; }

    public decimal? Rating { get; set; }

    public bool? IsVerified { get; set; }

    public virtual AppUser UserU { get; set; } = null!;
}
