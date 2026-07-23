namespace HomeServicesPortal.Models.Api;

/// <summary>Provider document paths and verification status returned to clients.</summary>
public class ProviderDocumentsApiDto
{
    public int ProviderUid { get; set; }

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
