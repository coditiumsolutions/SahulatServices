namespace HomeServicesPortal.Models.Entities;

public partial class CustomerProfile
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string? DefaultAddress { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? ProfileImage { get; set; }

    public virtual AppUser UserU { get; set; } = null!;
}
