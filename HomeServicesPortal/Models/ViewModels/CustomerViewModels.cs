using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class CustomerListVm
{
    public List<CustomerItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "name";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class CustomerItemVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public string? Gender { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class CustomerFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20)]
    [Display(Name = "Mobile No")]
    public string MobileNo { get; set; } = string.Empty;

    [StringLength(15)]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [StringLength(20)]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [StringLength(500)]
    [Display(Name = "Customer Alert")]
    public string? CustomerAlert { get; set; }

    [StringLength(1000)]
    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    [StringLength(100)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(250)]
    [Display(Name = "Location")]
    public string? Location { get; set; }

    public List<SelectListItem> CityOptions { get; set; } = new();
    public List<SelectListItem> LocationOptions { get; set; } = new();
    public List<SelectListItem> AlertOptions { get; set; } = new();
    public List<CustomerAddressItemVm> Addresses { get; set; } = new();
}

public class CustomerDetailsVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public string? Gender { get; set; }
    public string? CustomerAlert { get; set; }
    public string? Comments { get; set; }
    public string? City { get; set; }
    public string? Location { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int ServiceRequestCount { get; set; }
    public int AddressCount { get; set; }
    public List<CustomerAddressItemVm> Addresses { get; set; } = new();
}

public class CustomerAddressItemVm
{
    public int Uid { get; set; }
    public int ClientUid { get; set; }
    public string AddressTitle { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class CustomerAddressFormVm
{
    public int Uid { get; set; }

    public int ClientUid { get; set; }

    public string ClientName { get; set; } = string.Empty;

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

    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }
}

public class CustomerAddressDeleteVm
{
    public int Uid { get; set; }
    public int ClientUid { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string AddressTitle { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsLinkedToRequests { get; set; }
}

public class CustomerDeleteVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public int AddressCount { get; set; }
    public int ServiceRequestCount { get; set; }
    public int BookingCount { get; set; }
    public int PaymentLedgerCount { get; set; }
    public bool HasLinkedData => AddressCount > 0 || ServiceRequestCount > 0 || BookingCount > 0 || PaymentLedgerCount > 0;
}

public class CustomerRequestsListVm
{
    public List<CustomerRequestRowVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class CustomerRequestRowVm
{
    public int ClientUid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public int RequestUid { get; set; }
    public string ServiceTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public DateTime CreatedOn { get; set; }
}
