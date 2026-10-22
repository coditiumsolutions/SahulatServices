namespace HomeServicesPortal.Models.ViewModels;

public class PublicDashboardVm
{
    public string FullName { get; set; } = string.Empty;
    public string MobileNo { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public int ProfileId { get; set; }
    public DateTime? LastLogin { get; set; }
}
