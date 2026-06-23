using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface ICustomerService
{
    Task<CustomerListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<CustomerDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomerDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(CustomerFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(CustomerFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
