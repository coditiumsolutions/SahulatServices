namespace HomeServicesPortal.Models.ViewModels;

public class PreferencesVm
{
    /// <summary>True = 12-hour ("hh:mm tt"), false = 24-hour ("HH:mm"). Applies portal-wide.</summary>
    public bool Use12Hour { get; set; }
}
