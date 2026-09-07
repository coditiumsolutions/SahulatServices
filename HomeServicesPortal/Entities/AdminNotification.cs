namespace HomeServicesPortal.Entities;

/// <summary>Admin-portal notification row (e.g. new service request created).</summary>
public class AdminNotification
{
    public int Uid { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? LinkUrl { get; set; }

    public int? RelatedEntityUid { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedOn { get; set; }
}
