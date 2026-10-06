using System.ComponentModel.DataAnnotations;

namespace HomeServicesPortal.Models.ViewModels;

public class PushBroadcastFormVm
{
    [Required(ErrorMessage = "Choose a platform.")]
    [Display(Name = "Send to")]
    public string Platform { get; set; } = "all";

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(60, ErrorMessage = "Title can be at most 60 characters.")]
    public string Title { get; set; } = "Update available";

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(200, ErrorMessage = "Message can be at most 200 characters.")]
    public string Message { get; set; } = "A new version of Sahulat Ghar Tak is available. Update now for the latest features and fixes.";

    /// <summary>Number of registered devices per platform, shown so staff know the reach before sending.</summary>
    public int AndroidDevices { get; set; }

    public int IosDevices { get; set; }

    public bool FirebaseConfigured { get; set; }

    /// <summary>
    /// Version each platform's push announces. Pre-filled from AppConfig; editable because the two stores release
    /// independently. Only the targeted platform's value is used.
    /// </summary>
    [Display(Name = "Android latest version")]
    public string AndroidLatestVersion { get; set; } = string.Empty;

    [Display(Name = "iOS latest version")]
    public string IosLatestVersion { get; set; } = string.Empty;

    /// <summary>Per platform: ticked = the app blocks until updated, unticked = a dismissable "Update available" dialog.</summary>
    [Display(Name = "Force update")]
    public bool AndroidForceUpdate { get; set; } = true;

    [Display(Name = "Force update")]
    public bool IosForceUpdate { get; set; } = true;

    /// <summary>Store links the push carries (read from AppConfig, display only).</summary>
    public string AndroidStoreUrl { get; set; } = string.Empty;

    public string IosStoreUrl { get; set; } = string.Empty;

    /// <summary>Saved latest version per platform, used by the "typed version is ahead of the store" warning.</summary>
    public string AndroidSavedVersion { get; set; } = string.Empty;

    public string IosSavedVersion { get; set; } = string.Empty;

    // ---- "Release blocked devices" card ----
    public ReleaseFormVm Release { get; set; } = new();

    public List<PushTesterDeviceRow> ReleaseDevices { get; set; } = new();

    public List<ReleaseHistoryRow> ReleaseHistory { get; set; } = new();

    /// <summary>Set after a release was sent, for the result line (comes through TempData).</summary>
    public string? ReleaseMessage { get; set; }

    public string? ReleaseError { get; set; }
}

/// <summary>Posted by the "Release blocked devices" card.</summary>
public class ReleaseFormVm
{
    /// <summary>everyone | android | ios | user | device.</summary>
    public string Scope { get; set; } = "everyone";

    public int? UserId { get; set; }

    public int? TokenId { get; set; }

    [StringLength(200, ErrorMessage = "The reason can be at most 200 characters.")]
    public string Reason { get; set; } = string.Empty;
}

public class ReleaseHistoryRow
{
    public DateTime ReleasedAtUtc { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string ReleasedBy { get; set; } = string.Empty;
    public int Recipients { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
}
