using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface IPaymentService
{
    Task<PaymentLedgerListVm> GetLedgerListAsync(string? search, int page, CancellationToken cancellationToken = default);
    Task<PaymentLedgerDetailsVm?> GetLedgerDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ProviderPayoutListVm> GetPayoutListAsync(string? search, int page, CancellationToken cancellationToken = default);
    Task<ProviderPayoutDetailsVm?> GetPayoutDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Posts PaymentLedger (and ProviderPayout when OnlineToCompany) when a booking is Completed. Idempotent.</summary>
    Task<(bool Created, string? Message)> RecordBookingCompletionAsync(int bookingUid, CancellationToken cancellationToken = default);

    Task<(bool Created, string? Message)> RecordBookingCompletionAsync(ServiceBooking booking, CancellationToken cancellationToken = default);

    /// <summary>Posts finance for any Completed bookings that have no ledger rows yet.</summary>
    Task<int> SyncCompletedBookingsAsync(CancellationToken cancellationToken = default);

    Task<PersonLedgerIndexVm> GetPersonLedgerIndexAsync(string? search, CancellationToken cancellationToken = default);

    Task<PersonLedgerStatementVm?> GetPersonLedgerAsync(int providerUid, CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> AddPersonLedgerEntryAsync(PersonLedgerAddEntryVm model, CancellationToken cancellationToken = default);

    Task<CompanyLedgerVm> GetCompanyLedgerAsync(string? search, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pays out a provider's pending online earnings, netted against any commission they
    /// owe the company from cash jobs (i.e. their current ledger balance). Marks Pending
    /// ProviderPayout rows Paid and posts an offsetting Payout ledger entry for the amount
    /// actually disbursed.
    /// </summary>
    Task<(bool Success, string? Message, decimal AmountPaid)> PayProviderAsync(int providerUid, string? method, CancellationToken cancellationToken = default);

    Task<ProviderWalletVm?> GetProviderWalletAsync(int providerUid, CancellationToken cancellationToken = default);
}
