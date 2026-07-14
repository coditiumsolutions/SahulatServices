using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface IPaymentService
{
    Task<PaymentLedgerListVm> GetLedgerListAsync(string? search, int page, CancellationToken cancellationToken = default);
    Task<PaymentLedgerDetailsVm?> GetLedgerDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderPayoutListVm> GetPayoutListAsync(string? search, int page, CancellationToken cancellationToken = default);
    Task<ProviderPayoutDetailsVm?> GetPayoutDetailsAsync(int id, CancellationToken cancellationToken = default);
}
