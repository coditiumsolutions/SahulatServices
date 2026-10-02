using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.ViewModels;

public class PushTesterFormVm
{
    /// <summary>user = every device of one user, device = one registered device, role = every Client or Provider device.</summary>
    public string TargetMode { get; set; } = "user";

    [Display(Name = "User id")]
    public int? UserId { get; set; }

    [Display(Name = "Device")]
    public int? TokenId { get; set; }

    /// <summary>Client or Provider. For "role" targets it picks the audience; otherwise it is only used by events with no fixed role.</summary>
    public string Role { get; set; } = "Client";

    public string Platform { get; set; } = "all";

    public string EventKey { get; set; } = "job_assigned";

    [StringLength(100)]
    [Display(Name = "Service name")]
    public string? ServiceName { get; set; } = "AC Repair";

    [StringLength(100)]
    [Display(Name = "Provider name")]
    public string? ProviderName { get; set; } = "Test Provider";

    [StringLength(200)]
    [Display(Name = "Cancel reason")]
    public string? Reason { get; set; }

    [Display(Name = "Booking id")]
    public int? BookingId { get; set; }

    [Display(Name = "Request id")]
    public int? RequestId { get; set; }

    [StringLength(60)]
    [Display(Name = "Title")]
    public string? CustomTitle { get; set; }

    [StringLength(200)]
    [Display(Name = "Message")]
    public string? CustomBody { get; set; }

    [StringLength(40)]
    [Display(Name = "Type")]
    public string? CustomType { get; set; } = "custom";

    [StringLength(40)]
    [Display(Name = "Screen")]
    public string? CustomScreen { get; set; }

    [StringLength(500)]
    [Display(Name = "Extra data (key=value per line)")]
    public string? ExtraData { get; set; }

    [Display(Name = "Deliver to the user's devices of any role")]
    public bool IgnoreRole { get; set; } = true;

    [Display(Name = "Also save to the user's in-app inbox")]
    public bool SaveToInbox { get; set; }

    [Display(Name = "Send on the type's Android channel")]
    public bool UseAndroidChannel { get; set; }

    public bool FirebaseConfigured { get; set; }

    public List<PushTesterDeviceRow> Devices { get; set; } = new();

    /// <summary>What the last send used, shown under the form.</summary>
    public PushTesterResult? Result { get; set; }
}

public class PushTesterDeviceRow
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserType { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string? Name { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PushTesterResult
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Screen { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public bool ChannelSent { get; set; }
    public int Recipients { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int RemovedStale { get; set; }
    public bool InboxSaved { get; set; }
}
