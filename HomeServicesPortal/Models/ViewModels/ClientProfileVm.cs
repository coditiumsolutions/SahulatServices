using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.ViewModels;

public class ClientProfileVm
{
    public string FullName { get; set; } = string.Empty;
    public string MobileNo { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? LastLogin { get; set; }
    public IReadOnlyList<ClientAddressListItemVm> Addresses { get; set; } = Array.Empty<ClientAddressListItemVm>();
}

public class ClientAddressListItemVm
{
    public int Uid { get; set; }
    public string AddressTitle { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool InUse { get; set; }
}

public class ClientPortalAddressFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Address title is required.")]
    [StringLength(100)]
    [Display(Name = "Address Title")]
    public string AddressTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full address is required.")]
    [StringLength(500)]
    [Display(Name = "Full Address")]
    public string FullAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Area is required.")]
    [StringLength(150)]
    [Display(Name = "Area")]
    public string Area { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [StringLength(100)]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Display(Name = "Latitude (optional)")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Display(Name = "Longitude (optional)")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    public bool IsEdit => Uid > 0;
}

public class ClientPortalAddressDeleteVm
{
    public int Uid { get; set; }
    public string AddressTitle { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class ClientProfileEditVm
{
    [Required(ErrorMessage = "Required")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a gender.")]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    /// <summary>Read-only: the mobile number cannot be changed.</summary>
    public string MobileNo { get; set; } = string.Empty;
}
