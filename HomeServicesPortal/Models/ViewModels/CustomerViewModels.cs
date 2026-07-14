using System.ComponentModel.DataAnnotations;

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
}

public class CustomerDetailsVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public string? Gender { get; set; }
    public DateTime? CreatedOn { get; set; }
    public int ServiceRequestCount { get; set; }
    public int AddressCount { get; set; }
}

public class CustomerDeleteVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
}
