using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IProviderDocumentService
{
    Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default);
    List<SelectListItem> GetDocumentTypeOptions();
    Task<ProviderDocumentListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ProviderDocumentDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderDocumentFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderDocumentDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ProviderDocumentFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ProviderDocumentFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderDocumentFormVm> PopulateFormAsync(ProviderDocumentFormVm model, CancellationToken cancellationToken = default);
}
