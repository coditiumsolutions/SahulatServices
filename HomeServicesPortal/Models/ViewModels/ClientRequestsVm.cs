using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Models.ViewModels;

public class ClientRequestsVm
{
    public const string TabActive = "active";
    public const string TabCompleted = "completed";
    public const string TabCancelled = "cancelled";

    public string FullName { get; set; } = string.Empty;

    /// <summary>One of <see cref="TabActive"/>, <see cref="TabCompleted"/>, <see cref="TabCancelled"/>.</summary>
    public string Tab { get; set; } = TabActive;

    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }

    /// <summary>Requests for the selected tab only.</summary>
    public IReadOnlyList<ClientRequestCardVm> Requests { get; set; } = Array.Empty<ClientRequestCardVm>();
}

public class ClientRequestCardVm
{
    public int Uid { get; set; }
    public string ServiceTitle { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? AddressTitle { get; set; }
    public string? ServiceDescription { get; set; }
    public DateOnly? PreferredServiceDate { get; set; }
    public string? PreferredServiceTime { get; set; }
    public decimal? EstimatedBudget { get; set; }
    public bool IsUrgent { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProgressStatus { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderMobileNo { get; set; }
    public string? ProviderProfilePhotoPath { get; set; }
    public string? ProviderCnic { get; set; }
    public string? Passcode { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactNo { get; set; }
    public string? Remarks { get; set; }

    /// <summary>What the client sees: the computed progress stage, or "Cancelled" when it is null.</summary>
    public string StatusLabel => ProgressStatus ?? "Cancelled";

    /// <summary>The app lets the client cancel only while the raw status is Initiated (or the legacy Pending).</summary>
    public bool CanCancel =>
        string.Equals(Status, "Initiated", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);

    public bool HasProvider => !string.IsNullOrWhiteSpace(ProviderName) || !string.IsNullOrWhiteSpace(ProviderMobileNo);

    public static ClientRequestCardVm From(CustomerServiceRequestApiDto r) => new()
    {
        Uid = r.Uid,
        ServiceTitle = r.ServiceTitle,
        CategoryName = r.CategoryName,
        AddressTitle = r.AddressTitle,
        ServiceDescription = r.ServiceDescription,
        PreferredServiceDate = r.PreferredServiceDate,
        PreferredServiceTime = r.PreferredServiceTime,
        EstimatedBudget = r.EstimatedBudget,
        IsUrgent = r.IsUrgent,
        Status = r.Status,
        ProgressStatus = r.ProgressStatus,
        CreatedOn = r.CreatedOn,
        ProviderName = r.ProviderName,
        ProviderMobileNo = r.ProviderMobileNo,
        ProviderProfilePhotoPath = r.ProviderProfilePhotoPath,
        ProviderCnic = r.ProviderCnic,
        Passcode = r.Passcode,
        ContactPerson = r.ContactPerson,
        ContactNo = r.ContactNo,
        Remarks = r.Remarks
    };
}

public class CancelRequestForm
{
    public string? Reason { get; set; }
    public string? OtherReason { get; set; }
    public string? ReturnTo { get; set; }
}
