using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IProviderQuoteService
{
    Task<List<SelectListItem>> GetRequestOptionsAsync(CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default);
    Task<ProviderQuoteListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ProviderQuoteDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderQuoteFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderQuoteDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ProviderQuoteFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ProviderQuoteFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderQuoteFormVm> PopulateFormAsync(ProviderQuoteFormVm model, CancellationToken cancellationToken = default);
}
