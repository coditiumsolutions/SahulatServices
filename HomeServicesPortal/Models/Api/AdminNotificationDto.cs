namespace HomeServicesPortal.Models.Api;

public class AdminNotificationDto
{
    public int Uid { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public int? RelatedEntityUid { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Server-formatted PKT display string (e.g. "28-09-2026 14:05" or "02:05 PM" depending on
    /// the admin's Preferences > Time Format toggle) — see PktTimeHelper.ToPktDisplay(). The
    /// admin portal bell feed renders this directly instead of new Date(CreatedOn) client-side,
    /// which was silently rendering the raw UTC clock value as if it were already local time.
    /// </summary>
    public string CreatedOnDisplay { get; set; } = string.Empty;
}

public class AdminNotificationFeedDto
{
    public int UnreadCount { get; set; }
    public List<AdminNotificationDto> Items { get; set; } = new();
}
