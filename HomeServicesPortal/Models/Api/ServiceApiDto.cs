namespace HomeServicesPortal.Models.Api;

public class ServiceApiDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime? CreatedOn { get; set; }
}
