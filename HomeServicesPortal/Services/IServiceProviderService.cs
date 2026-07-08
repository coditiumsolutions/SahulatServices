using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IServiceProviderService
{
    Task<IReadOnlyList<ProviderProfileApiDto>> GetProviderProfilesForApiAsync(
        int? categoryId,
        CancellationToken cancellationToken = default);
    Task<ProviderProfileApiDto?> GetProviderProfileByUserUidAsync(
        int userUid,
        CancellationToken cancellationToken = default);
    Task<ProviderServiceRequestResponse> GetServiceRequestsForProviderAsync(
        int providerUid,
        CancellationToken cancellationToken = default);
    Task<ServiceProviderListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ServiceProviderDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceProviderFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceProviderDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ServiceProviderFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ServiceProviderFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
