using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.Api;

/// <summary>Admin request to verify or reject provider documents.</summary>
public class VerifyProviderDocumentsRequestDto
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProviderUid { get; set; }

    [Required]
    public bool IsVerified { get; set; }

    /// <summary>Staff / admin user id performing verification.</summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int VerifiedBy { get; set; }

    [MaxLength(500)]
    public string? VerificationRemarks { get; set; }
}
