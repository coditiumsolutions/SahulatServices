using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IServiceCategoryService
{
    Task<IReadOnlyList<ServiceCategoryApiDto>> GetActiveCategoriesForApiAsync(
        int? serviceUid = null,
        CancellationToken cancellationToken = default);

    Task<ServiceCategoryApiDto?> GetActiveCategoryForApiAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<SelectListItem>> GetServiceOptionsAsync(CancellationToken cancellationToken = default);

    Task<ServiceCategoryListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default);

    Task<ServiceCategoryDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceCategoryFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceCategoryDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> CreateAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceCategoryFormVm> PopulateFormAsync(
        ServiceCategoryFormVm model,
        CancellationToken cancellationToken = default);
}
