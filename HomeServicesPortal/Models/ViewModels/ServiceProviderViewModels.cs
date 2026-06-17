using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ServiceProviderListVm
{
    public List<ServiceProviderItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "name";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ServiceProviderItemVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ExperienceYears { get; set; }
    public decimal? Rating { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public string? ProfilePicturePath { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ServiceProviderFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150)]
    [Display(Name = "Name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Mobile No")]
    public string? MobileNo { get; set; }

    [StringLength(20)]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [Display(Name = "Category")]
    public int CategoryUid { get; set; }

    [Display(Name = "Experience Years")]
    [Range(0, 60)]
    public int? ExperienceYears { get; set; }

    [Display(Name = "Rating")]
    [Range(0, 5)]
    public decimal? Rating { get; set; }

    [Display(Name = "Is Verified")]
    public bool IsVerified { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePicture { get; set; }

    public string? ExistingProfilePicturePath { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
}

public class ServiceProviderDetailsVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
    public string? Cnic { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int? ExperienceYears { get; set; }
    public decimal? Rating { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public string? ProfilePicturePath { get; set; }
    public DateTime? CreatedOn { get; set; }
}

public class ServiceProviderDeleteVm
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? MobileNo { get; set; }
}
