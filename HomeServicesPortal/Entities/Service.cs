namespace HomeServicesPortal.Entities;

/// <summary>Top-level service offering (parent of ServiceCategories).</summary>
public class Service
{
    public int Uid { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOn { get; set; }

    public ICollection<ServiceCategory> Categories { get; set; } = new List<ServiceCategory>();
}
