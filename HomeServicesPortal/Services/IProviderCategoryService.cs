using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IProviderCategoryService
{
    /// <summary>List of category UIDs for a provider, most-primary-first.</summary>
    Task<List<int>> GetCategoryUidsAsync(int providerUid, CancellationToken cancellationToken = default);

    /// <summary>Category UID + name + IsPrimary flag for a provider, most-primary-first. Empty list if provider not found.</summary>
    Task<List<ProviderCategoryItemDto>> GetCategoriesAsync(int providerUid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full-replace of a provider's category set. Writes the ProviderCategories junction rows
    /// (adding/removing as needed) AND updates the deprecated Providers.CategoryUid scalar to
    /// match primaryCategoryUid, so both stay consistent. This is the only place that should
    /// ever write Providers.CategoryUid.
    /// </summary>
    Task<(bool Success, string? Error)> SyncCategoriesAsync(
        int providerUid,
        List<int> categoryUids,
        int primaryCategoryUid,
        CancellationToken cancellationToken = default);
}
