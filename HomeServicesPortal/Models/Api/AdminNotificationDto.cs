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
}

public class AdminNotificationFeedDto
{
    public int UnreadCount { get; set; }
    public List<AdminNotificationDto> Items { get; set; } = new();
}
