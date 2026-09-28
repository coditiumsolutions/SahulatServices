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

    /// <summary>
    /// Provider profile photo (jpg/jpeg/png, max 5 MB). Optional on edit — a provider that
    /// already has a documents row may omit this to leave the stored profile photo unchanged.
    /// Required for a provider's first-ever submission (no existing documents row yet).
    /// </summary>
    [FromForm(Name = "ProfilePhoto")]
    public IFormFile? ProfilePhoto { get; set; }

    /// <summary>
    /// CNIC front image (jpg/jpeg/png, max 5 MB). Optional on edit — omit to leave the stored
    /// image unchanged. Required for a provider's first-ever submission.
    /// </summary>
    [FromForm(Name = "CNICFront")]
    public IFormFile? CnicFront { get; set; }

    /// <summary>
    /// CNIC back image (jpg/jpeg/png, max 5 MB). Optional on edit — omit to leave the stored
    /// image unchanged. Required for a provider's first-ever submission.
    /// </summary>
    [FromForm(Name = "CNICBack")]
    public IFormFile? CnicBack { get; set; }

    /// <summary>
    /// Police verification certificate image (jpg/jpeg/png, max 5 MB). Added v3.20 — optional on
    /// both first submission and edit; older app builds that never send this simply omit it and
    /// nothing about the existing three-image flow changes.
    /// </summary>
    [FromForm(Name = "PoliceVerification")]
    public IFormFile? PoliceVerification { get; set; }
}
