namespace HomeServicesPortal.Entities;

/// <summary>
/// One document record per provider. Stores relative paths under wwwroot only
/// (e.g. uploads/providers/25/profile.jpg). Absolute URLs must never be stored.
/// </summary>
public class ProviderDocument
{
    public int Uid { get; set; }

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

    public Provider Provider { get; set; } = null!;
}
