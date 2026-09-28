namespace HomeServicesPortal.Models.Api;

public class ProviderServiceTitleItemDto
{
    public int ServiceTitleUid { get; set; }

    public string Title { get; set; } = string.Empty;

    public int CategoryUid { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}

public class UpdateProviderServiceTitlesRequestDto
{
    public List<int> ServiceTitleIds { get; set; } = new();
}
