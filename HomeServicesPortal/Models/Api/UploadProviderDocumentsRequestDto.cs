using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Models.Api;

/// <summary>Multipart form for uploading provider profile and CNIC images.</summary>
public class UploadProviderDocumentsRequestDto
{
    /// <summary>Target provider primary key (Providers.UID).</summary>
    [Required]
    [Range(1, int.MaxValue)]
    [FromForm(Name = "ProviderUID")]
    public int ProviderUid { get; set; }

    /// <summary>Provider profile photo (jpg/jpeg/png, max 5 MB).</summary>
    [Required]
    [FromForm(Name = "ProfilePhoto")]
    public IFormFile ProfilePhoto { get; set; } = null!;

    /// <summary>CNIC front image (jpg/jpeg/png, max 5 MB).</summary>
    [Required]
    [FromForm(Name = "CNICFront")]
    public IFormFile CnicFront { get; set; } = null!;

    /// <summary>CNIC back image (jpg/jpeg/png, max 5 MB).</summary>
    [Required]
    [FromForm(Name = "CNICBack")]
    public IFormFile CnicBack { get; set; } = null!;
}
