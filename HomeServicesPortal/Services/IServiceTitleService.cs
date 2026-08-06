using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IServiceTitleService
{
    Task<IReadOnlyList<ServiceTitleApiDto>> GetActiveTitlesForApiAsync(
        int categoryUid,
        CancellationToken cancellationToken = default);

    Task<ServiceTitleApiDto?> GetActiveTitleForApiAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default);

    Task<ServiceTitleFormVm> PopulateFormAsync(
        ServiceTitleFormVm model,
        CancellationToken cancellationToken = default);

    Task<ServiceTitleListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default);

    Task<ServiceTitleDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceTitleFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceTitleDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> CreateAsync(
        ServiceTitleFormVm model,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateAsync(
        ServiceTitleFormVm model,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
