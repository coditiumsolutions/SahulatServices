namespace HomeServicesPortal.Helpers;

/// <summary>
/// Status values a client is allowed to set on their own CustomerServiceRequests row via the
/// mobile PUT endpoint. Deliberately narrower than the admin portal's own whitelist
/// (Services/ServiceRequestService.ValidStatuses) — a client can edit while Initiated or cancel,
/// but Assigned/In Progress/Completed are staff/provider/system-driven only.
/// See docs/status-workflow.md.
/// </summary>
public static class RequestStatusConstants
{
    public const string Initiated = "Initiated";
    public const string Assigned = "Assigned";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    /// <summary>Legacy pre-assignment value; still accepted for older clients / rows.</summary>
    public const string LegacyPending = "Pending";

    public static readonly string[] ClientEditableStatuses =
    [
        Initiated,
        LegacyPending, // older Flutter builds may still send Pending when editing
        Cancelled
    ];

    /// <summary>Request has no active assignment yet (new, or provider rejected).</summary>
    public static bool IsUnassigned(string? status) =>
        string.Equals(status, Initiated, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, LegacyPending, StringComparison.OrdinalIgnoreCase);

    /// <summary>Normalize client/admin input: Pending → Initiated.</summary>
    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return Initiated;
        var trimmed = status.Trim();
        return string.Equals(trimmed, LegacyPending, StringComparison.OrdinalIgnoreCase)
            ? Initiated
            : trimmed;
    }
}
