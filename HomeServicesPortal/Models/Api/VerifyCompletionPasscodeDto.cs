namespace HomeServicesPortal.Models.Api;

public class VerifyCompletionPasscodeDto
{
    public int ProviderUid { get; set; }

    public string Passcode { get; set; } = string.Empty;

    public decimal ActualAmountPaid { get; set; }

    public string? PaymentMode { get; set; }

    // --- Added for labour/material split (2026-09-21) ---
    // Both OPTIONAL and additive: an app build that omits them keeps today's exact behavior
    // (ActualAmountPaid treated as the whole FinalAmount, commission computed on the whole
    // amount). Only once the app sends LabourAmount does completion switch to computing
    // commission off labour only, with materials passed through to the customer bill.
    // TODO(remove after old app retired): once every live app build always sends LabourAmount,
    // the "both null -> legacy whole-amount behavior" branch in
    // BookingService.VerifyCompletionPasscodeAsync can be deleted and these two fields made
    // effectively required (validate non-null instead of falling back).

    /// <summary>Labour-only charge collected on-site. Null = old app, legacy whole-amount behavior.</summary>
    public decimal? LabourAmount { get; set; }

    /// <summary>Itemized material breakdown. Null/empty is valid even when LabourAmount is sent (no materials used).</summary>
    public List<VerifyCompletionMaterialItemDto>? MaterialItems { get; set; }
}

public class VerifyCompletionMaterialItemDto
{
    public string ItemName { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }
}
