using HomeServicesPortal.Data;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentLedgerListVm> GetLedgerListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;

        var query = _db.PaymentLedgers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                l.AccountType.Contains(term) ||
                l.EntryType.Contains(term) ||
                l.Reason.Contains(term) ||
                (l.Provider != null && l.Provider.FullName.Contains(term)) ||
                (l.BookingUid != null && l.BookingUid.Value.ToString().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new PaymentLedgerItemVm
            {
                Uid = l.Uid,
                BookingUid = l.BookingUid,
                AccountType = l.AccountType,
                ProviderName = l.Provider != null ? l.Provider.FullName : null,
                EntryType = l.EntryType,
                Amount = l.Amount,
                Reason = l.Reason,
                CreatedOn = l.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new PaymentLedgerListVm
        {
            Items = items,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PaymentLedgerDetailsVm?> GetLedgerDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.PaymentLedgers
            .AsNoTracking()
            .Where(l => l.Uid == id)
            .Select(l => new PaymentLedgerDetailsVm
            {
                Uid = l.Uid,
                BookingUid = l.BookingUid,
                AccountType = l.AccountType,
                ProviderUid = l.ProviderUid,
                ProviderName = l.Provider != null ? l.Provider.FullName : null,
                EntryType = l.EntryType,
                Amount = l.Amount,
                Reason = l.Reason,
                CreatedOn = l.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProviderPayoutListVm> GetPayoutListAsync(
        string? search,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;

        var query = _db.ProviderPayouts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Provider.FullName.Contains(term) ||
                p.Status.Contains(term) ||
                (p.Method != null && p.Method.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProviderPayoutItemVm
            {
                Uid = p.Uid,
                ProviderName = p.Provider.FullName,
                Amount = p.Amount,
                Status = p.Status,
                Method = p.Method,
                CreatedOn = p.CreatedOn,
                PaidOn = p.PaidOn
            })
            .ToListAsync(cancellationToken);

        return new ProviderPayoutListVm
        {
            Items = items,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ProviderPayoutDetailsVm?> GetPayoutDetailsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.ProviderPayouts
            .AsNoTracking()
            .Where(p => p.Uid == id)
            .Select(p => new ProviderPayoutDetailsVm
            {
                Uid = p.Uid,
                ProviderUid = p.ProviderUid,
                ProviderName = p.Provider.FullName,
                Amount = p.Amount,
                Status = p.Status,
                Method = p.Method,
                CreatedOn = p.CreatedOn,
                PaidOn = p.PaidOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
