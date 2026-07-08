using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class PaymentService : IPaymentService
{
    private static readonly string[] ValidPaymentMethods =
        ["Cash", "Card", "Bank Transfer", "Mobile Wallet", "Other"];

    private readonly IRepository<Payment> _paymentRepo;
    private readonly IRepository<Booking> _bookingRepo;

    public PaymentService(
        IRepository<Payment> paymentRepo,
        IRepository<Booking> bookingRepo)
    {
        _paymentRepo = paymentRepo;
        _bookingRepo = bookingRepo;
    }

    public async Task<List<SelectListItem>> GetBookingOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _bookingRepo.Query()
            .OrderByDescending(b => b.BookingDate)
            .Select(b => new SelectListItem
            {
                Value = b.Uid.ToString(),
                Text = $"#{b.Uid} - {b.ProviderU.UserU.FullName} / Req #{b.RequestUid}"
            })
            .ToListAsync(cancellationToken);
    }

    public List<SelectListItem> GetPaymentMethodOptions()
    {
        return ValidPaymentMethods.Select(m => new SelectListItem { Value = m, Text = m }).ToList();
    }

    public async Task<PaymentListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "date" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var query = _paymentRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Bookin.ProviderU.UserU.FullName.Contains(term) ||
                (p.PaymentMethod != null && p.PaymentMethod.Contains(term)) ||
                (p.TransactionNo != null && p.TransactionNo.Contains(term)));
        }

        query = sort switch
        {
            "booking" => sortDir == "desc"
                ? query.OrderByDescending(p => p.BookingUid)
                : query.OrderBy(p => p.BookingUid),
            "amount" => sortDir == "desc"
                ? query.OrderByDescending(p => p.Amount)
                : query.OrderBy(p => p.Amount),
            "method" => sortDir == "desc"
                ? query.OrderByDescending(p => p.PaymentMethod)
                : query.OrderBy(p => p.PaymentMethod),
            _ => sortDir == "desc"
                ? query.OrderByDescending(p => p.PaymentDate)
                : query.OrderBy(p => p.PaymentDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentItemVm
            {
                Uid = p.Uid,
                BookingLabel = $"#{p.BookingUid} - {p.Bookin.ProviderU.UserU.FullName}",
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionNo = p.TransactionNo,
                PaymentDate = p.PaymentDate
            })
            .ToListAsync(cancellationToken);

        return new PaymentListVm
        {
            Items = items,
            Search = search,
            Sort = sort,
            SortDir = sortDir,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PaymentDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _paymentRepo.Query()
            .Where(p => p.Uid == id)
            .Select(p => new PaymentDetailsVm
            {
                Uid = p.Uid,
                BookingUid = p.BookingUid,
                BookingLabel = $"#{p.BookingUid} - {p.Bookin.ProviderU.UserU.FullName} / Req #{p.Bookin.RequestUid}",
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionNo = p.TransactionNo,
                PaymentDate = p.PaymentDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _paymentRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return await PopulateFormAsync(new PaymentFormVm
        {
            Uid = entity.Uid,
            BookingUid = entity.BookingUid,
            Amount = entity.Amount,
            PaymentMethod = entity.PaymentMethod,
            TransactionNo = entity.TransactionNo
        }, cancellationToken);
    }

    public async Task<PaymentDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _paymentRepo.Query()
            .Where(p => p.Uid == id)
            .Select(p => new PaymentDeleteVm
            {
                Uid = p.Uid,
                BookingLabel = $"#{p.BookingUid} - {p.Bookin.ProviderU.UserU.FullName}",
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                PaymentDate = p.PaymentDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        PaymentFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        var entity = new Payment
        {
            BookingUid = model.BookingUid,
            Amount = model.Amount,
            PaymentMethod = model.PaymentMethod?.Trim(),
            TransactionNo = model.TransactionNo?.Trim(),
            PaymentDate = DateTime.Now
        };

        await _paymentRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        PaymentFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _paymentRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null) return (false, "Payment not found.");

        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        entity.BookingUid = model.BookingUid;
        entity.Amount = model.Amount;
        entity.PaymentMethod = model.PaymentMethod?.Trim();
        entity.TransactionNo = model.TransactionNo?.Trim();

        await _paymentRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _paymentRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return (false, "Payment not found.");

        await _paymentRepo.DeleteAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<PaymentFormVm> PopulateFormAsync(
        PaymentFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Bookings = await GetBookingOptionsAsync(cancellationToken);
        model.PaymentMethods = GetPaymentMethodOptions();
        return model;
    }

    private async Task<string?> ValidateAsync(PaymentFormVm model, CancellationToken cancellationToken)
    {
        var bookingExists = await _bookingRepo.Query()
            .AnyAsync(b => b.Uid == model.BookingUid, cancellationToken);
        if (!bookingExists) return "Selected booking does not exist.";

        if (!string.IsNullOrWhiteSpace(model.PaymentMethod) &&
            !ValidPaymentMethods.Contains(model.PaymentMethod))
        {
            return "Invalid payment method.";
        }

        return null;
    }
}
