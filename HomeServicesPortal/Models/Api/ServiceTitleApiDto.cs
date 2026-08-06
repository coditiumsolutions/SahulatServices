namespace HomeServicesPortal.Models.Api;

public class ServiceTitleApiDto
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime? CreatedOn { get; set; }
}
