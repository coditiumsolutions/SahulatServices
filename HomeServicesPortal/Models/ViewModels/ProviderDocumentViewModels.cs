using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class ProviderDocumentListVm
{
    public List<ProviderDocumentItemVm> Items { get; set; } = new();
    public string? Search { get; set; }
    public string Sort { get; set; } = "provider";
    public string SortDir { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class ProviderDocumentItemVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProfilePhotoPath { get; set; }
    public string? CnicFrontImagePath { get; set; }
    public string? CnicBackImagePath { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public bool HasAllImages =>
        !string.IsNullOrWhiteSpace(ProfilePhotoPath)
        && !string.IsNullOrWhiteSpace(CnicFrontImagePath)
        && !string.IsNullOrWhiteSpace(CnicBackImagePath);
}

public class ProviderDocumentFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [Display(Name = "Profile Photo")]
    public IFormFile? ProfilePhoto { get; set; }

    [Display(Name = "CNIC Front")]
    public IFormFile? CnicFront { get; set; }

    [Display(Name = "CNIC Back")]
    public IFormFile? CnicBack { get; set; }

    public string? ExistingProfilePhotoPath { get; set; }
    public string? ExistingCnicFrontPath { get; set; }
    public string? ExistingCnicBackPath { get; set; }

    [Display(Name = "Verified")]
    public bool IsVerified { get; set; }

    [StringLength(500)]
    [Display(Name = "Verification Remarks")]
    public string? VerificationRemarks { get; set; }

    public List<SelectListItem> Providers { get; set; } = new();
}

public class ProviderDocumentDetailsVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProfilePhotoPath { get; set; }
    public string? CnicFrontImagePath { get; set; }
    public string? CnicBackImagePath { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedOn { get; set; }
    public int? VerifiedBy { get; set; }
    public string? VerificationRemarks { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}

public class ProviderDocumentDeleteVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProfilePhotoPath { get; set; }
    public string? CnicFrontImagePath { get; set; }
    public string? CnicBackImagePath { get; set; }
    public bool IsVerified { get; set; }
}
