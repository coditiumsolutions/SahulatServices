namespace HomeServicesPortal.Entities;

public class ServiceCategory
{
    public int Uid { get; set; }

    public int ServiceUid { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOn { get; set; }

    public Service Service { get; set; } = null!;

    public ICollection<Provider> Providers { get; set; } = new List<Provider>();

    public ICollection<ServiceTitle> Titles { get; set; } = new List<ServiceTitle>();
}
