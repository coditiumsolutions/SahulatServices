using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface IServiceService
{
    Task<ServiceListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default);

    Task<ServiceDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> CreateAsync(
        ServiceFormVm model,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateAsync(
        ServiceFormVm model,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
