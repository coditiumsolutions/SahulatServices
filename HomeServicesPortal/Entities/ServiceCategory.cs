namespace HomeServicesPortal.Entities;

public class ServiceCategory
{
    public int Uid { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOn { get; set; }

    public ICollection<Provider> Providers { get; set; } = new List<Provider>();
}
