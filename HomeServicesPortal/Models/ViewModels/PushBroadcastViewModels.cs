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
}
