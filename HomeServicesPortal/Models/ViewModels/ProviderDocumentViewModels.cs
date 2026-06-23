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
    public string ProviderName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string? FilePath { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateOnly.FromDateTime(DateTime.Today);
}

public class ProviderDocumentFormVm
{
    public int Uid { get; set; }

    [Required(ErrorMessage = "Provider is required.")]
    [Display(Name = "Provider")]
    public int ProviderUid { get; set; }

    [StringLength(100)]
    [Display(Name = "Document Type")]
    public string? DocumentType { get; set; }

    [StringLength(100)]
    [Display(Name = "Document No")]
    public string? DocumentNo { get; set; }

    [Display(Name = "Document File")]
    public IFormFile? DocumentFile { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Expiry Date")]
    public DateOnly? ExpiryDate { get; set; }

    public string? ExistingFilePath { get; set; }

    public List<SelectListItem> Providers { get; set; } = new();
    public List<SelectListItem> DocumentTypes { get; set; } = new();
}

public class ProviderDocumentDetailsVm
{
    public int Uid { get; set; }
    public int ProviderUid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string? FilePath { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateOnly.FromDateTime(DateTime.Today);
}

public class ProviderDocumentDeleteVm
{
    public int Uid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public string? DocumentNo { get; set; }
    public string? FilePath { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
