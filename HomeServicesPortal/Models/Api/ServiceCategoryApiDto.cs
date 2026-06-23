namespace HomeServicesPortal.Models.Api;

public class ServiceCategoryApiDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? CreatedOn { get; set; }
}
