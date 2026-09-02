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

    public virtual ServiceCategory? CategoryU { get; set; }

    public virtual ICollection<ProviderAvailability> ProviderAvailabilities { get; set; } = new List<ProviderAvailability>();

    public virtual ICollection<ProviderDocument> ProviderDocuments { get; set; } = new List<ProviderDocument>();

    public virtual ICollection<ProviderLocation> ProviderLocations { get; set; } = new List<ProviderLocation>();

    public virtual ICollection<ProviderQuote> ProviderQuotes { get; set; } = new List<ProviderQuote>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
