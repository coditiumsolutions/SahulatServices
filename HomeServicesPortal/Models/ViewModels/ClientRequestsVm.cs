namespace HomeServicesPortal.Models.ViewModels;

public class ClientRequestsVm
{
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<ClientRequestCardVm> Requests { get; set; } = Array.Empty<ClientRequestCardVm>();
}

public class ClientRequestCardVm
{
    public int Uid { get; set; }
    public string ServiceTitle { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? AddressTitle { get; set; }
    public string? ServiceDescription { get; set; }
    public decimal? EstimatedBudget { get; set; }
    public bool IsUrgent { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProgressStatus { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? ProviderName { get; set; }
}
