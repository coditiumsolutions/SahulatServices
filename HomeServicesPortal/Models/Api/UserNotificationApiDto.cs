namespace HomeServicesPortal.Models.Api;

public class UserNotificationApiDto
{
    public int Id { get; set; }

    public string UserType { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Screen { get; set; } = string.Empty;

    public int? BookingUid { get; set; }

    public int? RequestUid { get; set; }

    public bool IsRead { get; set; }

    // Stored as UTC but read back with Kind=Unspecified, which serialises without a "Z" and which clients parse
    // as local time. Mark it UTC so the JSON carries the "Z" api.txt documents.
    private DateTime _createdAt;

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}

public class UserNotificationListDto
{
    public IReadOnlyList<UserNotificationApiDto> Items { get; set; } = Array.Empty<UserNotificationApiDto>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int UnreadCount { get; set; }
}

public class UnreadCountApiDto
{
    public int UnreadCount { get; set; }
}

public class MarkNotificationReadRequestDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "userId is required.")]
    public int UserId { get; set; }
}

public class MarkAllNotificationsReadRequestDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "userId is required.")]
    public int UserId { get; set; }

    /// <summary>Optional role filter: Client or Provider. Omit to mark every role's notifications.</summary>
    public string? UserType { get; set; }
}
