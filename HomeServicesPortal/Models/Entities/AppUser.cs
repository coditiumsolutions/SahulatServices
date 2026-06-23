namespace HomeServicesPortal.Models.Entities;

public partial class AppUser
{
    public int Uid { get; set; }

    public string? FullName { get; set; }

    public string? MobileNo { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public string? UserType { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedOn { get; set; }

    public virtual CustomerProfile? CustomerProfile { get; set; }

    public virtual ProviderProfile? ProviderProfile { get; set; }
}
