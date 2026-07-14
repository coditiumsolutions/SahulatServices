using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IServiceRequestService
{
    Task<List<SelectListItem>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default);
    List<SelectListItem> GetStatusOptions();
    Task<ServiceRequestListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ServiceRequestDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceRequestFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceRequestDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ServiceRequestFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ServiceRequestFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceRequestFormVm> PopulateFormAsync(ServiceRequestFormVm model, CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetAddressOptionsAsync(int clientUid, CancellationToken cancellationToken = default);
}
