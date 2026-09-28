namespace HomeServicesPortal.Models.Api;

public class ProviderCategoryItemDto
{
    public int CategoryUid { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}

public class UpdateProviderCategoriesRequestDto
{
    public List<int> CategoryIds { get; set; } = new();

    public int PrimaryCategoryId { get; set; }
}
