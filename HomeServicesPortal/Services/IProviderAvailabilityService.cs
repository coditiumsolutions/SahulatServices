using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IProviderAvailabilityService
{
    Task<ProviderAvailabilityListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ProviderAvailabilityDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderAvailabilityFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderAvailabilityDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ProviderAvailabilityFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ProviderAvailabilityFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
