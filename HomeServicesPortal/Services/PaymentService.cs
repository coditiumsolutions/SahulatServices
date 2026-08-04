using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
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
        // Catch up finance rows for bookings already marked Completed.
        await SyncCompletedBookingsAsync(cancellationToken);

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
        await SyncCompletedBookingsAsync(cancellationToken);

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

    public async Task<(bool Created, string? Message)> RecordBookingCompletionAsync(
        int bookingUid,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.ServiceBookings
            .FirstOrDefaultAsync(b => b.Uid == bookingUid, cancellationToken);

        if (booking == null)
        {
            return (false, "Booking not found.");
        }

        return await RecordBookingCompletionAsync(booking, cancellationToken);
    }

    public async Task<(bool Created, string? Message)> RecordBookingCompletionAsync(
        ServiceBooking booking,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(booking.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Booking is not completed.");
        }

        var alreadyPosted = await _db.PaymentLedgers
            .AnyAsync(l => l.BookingUid == booking.Uid, cancellationToken);
        if (alreadyPosted)
        {
            return (false, "Finance entries already exist for this booking.");
        }

        if (booking.CommissionAmount < 0 || booking.ProviderEarning < 0 || booking.CustomerPaid < 0)
        {
            return (false, "Booking amounts cannot be negative.");
        }

        var now = DateTime.Now;
        var entries = new List<PaymentLedger>();

        // Settlement split from Final Bill (Commission + Provider Earning).
        if (booking.CommissionAmount > 0)
        {
            entries.Add(new PaymentLedger
            {
                BookingUid = booking.Uid,
                AccountType = "Company",
                ProviderUid = null,
                EntryType = "Credit",
                Amount = booking.CommissionAmount,
                Reason = "Commission",
                CreatedOn = now
            });
        }

        if (booking.ProviderEarning > 0)
        {
            entries.Add(new PaymentLedger
            {
                BookingUid = booking.Uid,
                AccountType = "Provider",
                ProviderUid = booking.ProviderUid,
                EntryType = "Credit",
                Amount = booking.ProviderEarning,
                Reason = "JobEarning",
                CreatedOn = now
            });
        }

        // Customer payment movement (uses CustomerPaid + PaymentMethod from booking).
        if (booking.CustomerPaid > 0)
        {
            if (string.Equals(booking.PaymentMode, "OnlineToCompany", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(new PaymentLedger
                {
                    BookingUid = booking.Uid,
                    AccountType = "Company",
                    ProviderUid = null,
                    EntryType = "Credit",
                    Amount = booking.CustomerPaid,
                    Reason = "CustPayment",
                    CreatedOn = now
                });
            }
            else
            {
                // Provider already holds this cash in hand — it offsets what the company owes
                // him for the job (JobEarning credit above), so it posts as a Debit, not a
                // second Credit. Without this, cash-to-provider jobs double-count the same
                // money as both "earned" and "collected" credits.
                entries.Add(new PaymentLedger
                {
                    BookingUid = booking.Uid,
                    AccountType = "Provider",
                    ProviderUid = booking.ProviderUid,
                    EntryType = "Debit",
                    Amount = booking.CustomerPaid,
                    Reason = "CashCollect",
                    CreatedOn = now
                });
            }
        }

        if (entries.Count == 0)
        {
            return (false, "No finance amounts to post for this booking.");
        }

        _db.PaymentLedgers.AddRange(entries);

        // Online: company holds customer money → provider payout is pending.
        if (string.Equals(booking.PaymentMode, "OnlineToCompany", StringComparison.OrdinalIgnoreCase)
            && booking.ProviderEarning > 0)
        {
            _db.ProviderPayouts.Add(new ProviderPayout
            {
                ProviderUid = booking.ProviderUid,
                Amount = booking.ProviderEarning,
                Status = "Pending",
                Method = "Online",
                CreatedOn = now,
                PaidOn = null
            });
        }
        // Cash to provider: provider remits company commission (pending collection).
        else if (string.Equals(booking.PaymentMode, "CashToProvider", StringComparison.OrdinalIgnoreCase)
                 && booking.CommissionAmount > 0)
        {
            // Track remittance as company credit already posted; no ProviderPayout needed.
            // Provider owes company CommissionAmount from cash held.
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (true, $"Posted {entries.Count} ledger entr{(entries.Count == 1 ? "y" : "ies")} for booking #{booking.Uid}.");
    }

    public async Task<int> SyncCompletedBookingsAsync(CancellationToken cancellationToken = default)
    {
        var pendingBookingIds = await _db.ServiceBookings
            .AsNoTracking()
            .Where(b => b.Status == "Completed")
            .Where(b => !_db.PaymentLedgers.Any(l => l.BookingUid == b.Uid))
            .Select(b => b.Uid)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var bookingUid in pendingBookingIds)
        {
            var (ok, _) = await RecordBookingCompletionAsync(bookingUid, cancellationToken);
            if (ok) created++;
        }

        return created;
    }

    public async Task<PersonLedgerIndexVm> GetPersonLedgerIndexAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        await SyncCompletedBookingsAsync(cancellationToken);

        var providersQuery = _db.Providers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            providersQuery = providersQuery.Where(p =>
                p.FullName.Contains(term) ||
                p.Category.CategoryName.Contains(term));
        }

        var providers = await providersQuery
            .OrderBy(p => p.FullName)
            .Select(p => new
            {
                p.Uid,
                p.FullName,
                CategoryName = p.Category.CategoryName
            })
            .ToListAsync(cancellationToken);

        var ledgerByProvider = await _db.PaymentLedgers
            .AsNoTracking()
            .Where(l => l.ProviderUid != null && l.AccountType == "Provider")
            .GroupBy(l => l.ProviderUid!.Value)
            .Select(g => new
            {
                ProviderUid = g.Key,
                TransactionCount = g.Count(),
                TotalPlus = g.Where(x => x.EntryType == "Credit").Sum(x => x.Amount),
                TotalMinus = g.Where(x => x.EntryType == "Debit").Sum(x => x.Amount)
            })
            .ToListAsync(cancellationToken);

        var ledgerMap = ledgerByProvider.ToDictionary(x => x.ProviderUid);

        var people = providers.Select(p =>
        {
            ledgerMap.TryGetValue(p.Uid, out var stats);
            var plus = stats?.TotalPlus ?? 0;
            var minus = stats?.TotalMinus ?? 0;
            return new PersonLedgerPersonVm
            {
                ProviderUid = p.Uid,
                ProviderName = p.FullName,
                CategoryName = p.CategoryName,
                TransactionCount = stats?.TransactionCount ?? 0,
                TotalPlus = plus,
                TotalMinus = minus,
                Balance = plus - minus
            };
        }).ToList();

        return new PersonLedgerIndexVm
        {
            Search = search,
            People = people
        };
    }

    public async Task<PersonLedgerStatementVm?> GetPersonLedgerAsync(
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        await SyncCompletedBookingsAsync(cancellationToken);

        var provider = await _db.Providers
            .AsNoTracking()
            .Where(p => p.Uid == providerUid)
            .Select(p => new
            {
                p.Uid,
                p.FullName,
                CategoryName = p.Category.CategoryName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null) return null;

        var rows = await _db.PaymentLedgers
            .AsNoTracking()
            .Where(l => l.ProviderUid == providerUid && l.AccountType == "Provider")
            .OrderBy(l => l.CreatedOn)
            .ThenBy(l => l.Uid)
            .Select(l => new
            {
                l.Uid,
                l.CreatedOn,
                l.BookingUid,
                l.Reason,
                l.EntryType,
                l.Amount
            })
            .ToListAsync(cancellationToken);

        decimal running = 0;
        var txns = new List<PersonLedgerTxnVm>();
        foreach (var row in rows)
        {
            var signed = string.Equals(row.EntryType, "Debit", StringComparison.OrdinalIgnoreCase)
                ? -row.Amount
                : row.Amount;
            running += signed;
            txns.Add(new PersonLedgerTxnVm
            {
                Uid = row.Uid,
                CreatedOn = row.CreatedOn,
                BookingUid = row.BookingUid,
                Reason = row.Reason,
                EntryType = row.EntryType,
                Amount = row.Amount,
                SignedAmount = signed,
                RunningBalance = running
            });
        }

        var totalPlus = txns.Where(t => t.SignedAmount > 0).Sum(t => t.SignedAmount);
        var totalMinus = txns.Where(t => t.SignedAmount < 0).Sum(t => -t.SignedAmount);

        return new PersonLedgerStatementVm
        {
            ProviderUid = provider.Uid,
            ProviderName = provider.FullName,
            CategoryName = provider.CategoryName,
            TotalPlus = totalPlus,
            TotalMinus = totalMinus,
            Balance = running,
            Transactions = txns,
            AddEntry = new PersonLedgerAddEntryVm { ProviderUid = provider.Uid }
        };
    }

    public async Task<(bool Success, string? Message, decimal AmountPaid)> PayProviderAsync(
        int providerUid,
        string? method,
        CancellationToken cancellationToken = default)
    {
        await SyncCompletedBookingsAsync(cancellationToken);

        var providerExists = await _db.Providers.AnyAsync(p => p.Uid == providerUid, cancellationToken);
        if (!providerExists)
        {
            return (false, "Provider not found.", 0);
        }

        var balance = await _db.PaymentLedgers
            .AsNoTracking()
            .Where(l => l.ProviderUid == providerUid && l.AccountType == "Provider")
            .SumAsync(l => l.EntryType == "Debit" ? -l.Amount : l.Amount, cancellationToken);

        var pendingPayouts = await _db.ProviderPayouts
            .Where(p => p.ProviderUid == providerUid && p.Status == "Pending")
            .ToListAsync(cancellationToken);

        if (pendingPayouts.Count == 0 && balance <= 0)
        {
            return (false, "No pending payout and no positive balance to pay out.", 0);
        }

        // Amount actually disbursed is capped by what's owed overall (balance can be lower
        // than the sum of pending payouts if the provider also owes cash-job commission).
        var amountToPay = Math.Max(0, balance);

        var now = DateTime.Now;

        foreach (var payout in pendingPayouts)
        {
            payout.Status = "Paid";
            payout.PaidOn = now;
            payout.Method = string.IsNullOrWhiteSpace(method) ? payout.Method : method;
        }

        if (amountToPay > 0)
        {
            _db.PaymentLedgers.Add(new PaymentLedger
            {
                BookingUid = null,
                AccountType = "Provider",
                ProviderUid = providerUid,
                EntryType = "Debit",
                Amount = amountToPay,
                Reason = "Payout",
                CreatedOn = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        var message = amountToPay > 0
            ? $"Paid out {amountToPay:N2} to provider."
            : "Pending payouts cleared; no cash amount owed (offset by outstanding commission).";

        return (true, message, amountToPay);
    }

    public async Task<ProviderWalletVm?> GetProviderWalletAsync(
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        await SyncCompletedBookingsAsync(cancellationToken);

        var provider = await _db.Providers
            .AsNoTracking()
            .Where(p => p.Uid == providerUid)
            .Select(p => new { p.Uid, p.FullName, CategoryName = p.Category.CategoryName })
            .FirstOrDefaultAsync(cancellationToken);

        if (provider == null) return null;

        var rows = await _db.PaymentLedgers
            .AsNoTracking()
            .Where(l => l.ProviderUid == providerUid && l.AccountType == "Provider")
            .OrderBy(l => l.CreatedOn)
            .ThenBy(l => l.Uid)
            .Select(l => new { l.Uid, l.CreatedOn, l.BookingUid, l.Reason, l.EntryType, l.Amount })
            .ToListAsync(cancellationToken);

        decimal running = 0;
        var txns = new List<PersonLedgerTxnVm>();
        foreach (var row in rows)
        {
            var signed = string.Equals(row.EntryType, "Debit", StringComparison.OrdinalIgnoreCase) ? -row.Amount : row.Amount;
            running += signed;
            txns.Add(new PersonLedgerTxnVm
            {
                Uid = row.Uid,
                CreatedOn = row.CreatedOn,
                BookingUid = row.BookingUid,
                Reason = row.Reason,
                EntryType = row.EntryType,
                Amount = row.Amount,
                SignedAmount = signed,
                RunningBalance = running
            });
        }
        txns.Reverse();

        var pendingPayoutTotal = await _db.ProviderPayouts
            .AsNoTracking()
            .Where(p => p.ProviderUid == providerUid && p.Status == "Pending")
            .SumAsync(p => p.Amount, cancellationToken);

        return new ProviderWalletVm
        {
            ProviderUid = provider.Uid,
            ProviderName = provider.FullName,
            CategoryName = provider.CategoryName,
            Balance = running,
            PendingPayoutTotal = pendingPayoutTotal,
            Transactions = txns
        };
    }

    public async Task<(bool Success, string? Error)> AddPersonLedgerEntryAsync(
        PersonLedgerAddEntryVm model,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(model.EntryType, "Credit", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(model.EntryType, "Debit", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Entry type must be Credit (+) or Debit (−).");
        }

        var providerExists = await _db.Providers
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (!providerExists)
        {
            return (false, "Provider not found.");
        }

        if (model.BookingUid.HasValue)
        {
            var bookingOk = await _db.ServiceBookings
                .AnyAsync(b => b.Uid == model.BookingUid.Value, cancellationToken);
            if (!bookingOk)
            {
                return (false, "Booking not found.");
            }
        }

        var reason = model.Reason.Trim();
        if (reason.Length > 30)
        {
            reason = reason[..30];
        }

        _db.PaymentLedgers.Add(new PaymentLedger
        {
            BookingUid = model.BookingUid,
            AccountType = "Provider",
            ProviderUid = model.ProviderUid,
            EntryType = model.EntryType.Equals("Debit", StringComparison.OrdinalIgnoreCase) ? "Debit" : "Credit",
            Amount = model.Amount,
            Reason = reason,
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<CompanyLedgerVm> GetCompanyLedgerAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        await SyncCompletedBookingsAsync(cancellationToken);

        var query = _db.PaymentLedgers.AsNoTracking().Where(l => l.AccountType == "Company");

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(l =>
                l.Reason.Contains(term) ||
                (l.BookingUid != null && l.BookingUid.Value.ToString().Contains(term)));
        }

        var rows = await query
            .OrderBy(l => l.CreatedOn)
            .ThenBy(l => l.Uid)
            .Select(l => new
            {
                l.Uid,
                l.CreatedOn,
                l.BookingUid,
                l.Reason,
                l.EntryType,
                l.Amount
            })
            .ToListAsync(cancellationToken);

        decimal running = 0;
        var txns = new List<PersonLedgerTxnVm>();
        foreach (var row in rows)
        {
            var signed = string.Equals(row.EntryType, "Debit", StringComparison.OrdinalIgnoreCase)
                ? -row.Amount
                : row.Amount;
            running += signed;
            txns.Add(new PersonLedgerTxnVm
            {
                Uid = row.Uid,
                CreatedOn = row.CreatedOn,
                BookingUid = row.BookingUid,
                Reason = row.Reason,
                EntryType = row.EntryType,
                Amount = row.Amount,
                SignedAmount = signed,
                RunningBalance = running
            });
        }

        txns.Reverse();

        var totalPlus = txns.Where(t => t.SignedAmount > 0).Sum(t => t.SignedAmount);
        var totalMinus = txns.Where(t => t.SignedAmount < 0).Sum(t => -t.SignedAmount);

        return new CompanyLedgerVm
        {
            Search = search,
            TotalPlus = totalPlus,
            TotalMinus = totalMinus,
            Balance = running,
            Transactions = txns
        };
    }
}
