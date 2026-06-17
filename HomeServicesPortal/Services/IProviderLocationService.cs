using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IProviderLocationService
{
    Task<ProviderLocationListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ProviderLocationDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderLocationFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderLocationDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ProviderLocationFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ProviderLocationFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
