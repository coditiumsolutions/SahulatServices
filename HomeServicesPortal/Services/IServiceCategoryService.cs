using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface IServiceCategoryService
{
    Task<IReadOnlyList<ServiceCategoryApiDto>> GetActiveCategoriesForApiAsync(CancellationToken cancellationToken = default);
    Task<ServiceCategoryApiDto?> GetActiveCategoryForApiAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceCategoryListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceCategoryFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceCategoryDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ServiceCategoryFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ServiceCategoryFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
