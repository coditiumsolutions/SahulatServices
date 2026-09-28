using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IProviderServiceTitleService
{
    /// <summary>List of service title UIDs a provider offers. Empty list is a normal, unrestricted state.</summary>
    Task<List<int>> GetServiceTitleUidsAsync(int providerUid, CancellationToken cancellationToken = default);

    /// <summary>Service title UID + title + category for a provider, ordered by category then title.</summary>
    Task<List<ProviderServiceTitleItemDto>> GetServiceTitlesAsync(int providerUid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full-replace of a provider's service-title set. Unlike SyncCategoriesAsync, an empty list
    /// is valid (removes all titles). Every title must be active and belong (via
    /// ServiceTitle.CategoryUid) to one of the provider's current ProviderCategories rows.
    /// </summary>
    Task<(bool Success, string? Error)> SyncServiceTitlesAsync(
        int providerUid,
        List<int> serviceTitleUids,
        CancellationToken cancellationToken = default);
}
