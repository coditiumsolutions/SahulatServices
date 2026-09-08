using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface IConfigurationEntryService
{
    Task<ConfigurationListVm> GetListAsync(string? search, int page, CancellationToken cancellationToken = default);
    Task<ConfigurationDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ConfigurationFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ConfigurationDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ConfigurationFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ConfigurationFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetValuesByKeyAsync(string configKey, CancellationToken cancellationToken = default);
}
