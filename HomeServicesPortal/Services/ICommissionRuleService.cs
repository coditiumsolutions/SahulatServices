using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface ICommissionRuleService
{
    Task<CommissionRuleListVm> GetListAsync(string? search, int page, CancellationToken cancellationToken = default);
    Task<CommissionRuleDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<CommissionRuleFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<CommissionRuleDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<CommissionRuleFormVm> PopulateFormAsync(CommissionRuleFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(CommissionRuleFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(CommissionRuleFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Most-specific active rule wins: Provider → Category → Global.
    /// Maps Percentage → Percent for Assign forms.
    /// </summary>
    Task<ResolvedCommissionVm> ResolveAsync(
        int? providerUid,
        int? categoryUid,
        DateTime? asOf = null,
        CancellationToken cancellationToken = default);
}
