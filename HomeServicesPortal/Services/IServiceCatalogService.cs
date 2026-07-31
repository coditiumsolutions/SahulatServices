using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceApiDto>> GetActiveServicesForApiAsync(
        CancellationToken cancellationToken = default);

    Task<ServiceApiDto?> GetActiveServiceForApiAsync(
        int id,
        CancellationToken cancellationToken = default);
}
