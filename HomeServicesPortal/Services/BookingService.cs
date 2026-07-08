using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HomeServicesPortal.Helpers;
namespace HomeServicesPortal.Services;

public class BookingService : IBookingService
{
    private static readonly string[] ValidStatuses = ["Accepted", "In Progress", "Completed", "Cancelled"];

    private readonly IRepository<Booking> _bookingRepo;
    private readonly IRepository<ServiceRequest> _requestRepo;
    private readonly IRepository<ProviderProfile> _providerRepo;

    public BookingService(
        IRepository<Booking> bookingRepo,
        IRepository<ServiceRequest> requestRepo,
        IRepository<ProviderProfile> providerRepo)
    {
        _bookingRepo = bookingRepo;
        _requestRepo = requestRepo;
        _providerRepo = providerRepo;
    }

    public async Task<List<SelectListItem>> GetRequestOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _requestRepo.Query()
            .OrderByDescending(r => r.RequestDate)
            .Select(r => new SelectListItem
            {
                Value = r.Uid.ToString(),
                Text = $"#{r.Uid} - {r.CustomerU.FullName} ({r.CategoryU.CategoryName})"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _providerRepo.Query()
            .Where(p => p.UserU.IsActive != false && p.UserU.UserType == UserTypeConstants.Provider)
            .OrderBy(p => p.UserU.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.UserU.FullName ?? "—"
            })
            .ToListAsync(cancellationToken);
    }

    public List<SelectListItem> GetStatusOptions()
    {
        return ValidStatuses.Select(s => new SelectListItem { Value = s, Text = s }).ToList();
    }

    public async Task<BookingListVm> GetListAsync(
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

        var query = _bookingRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                b.RequestU.CustomerU.FullName.Contains(term) ||
                b.RequestU.CategoryU.CategoryName.Contains(term) ||
                b.ProviderU.UserU.FullName.Contains(term) ||
                (b.Status != null && b.Status.Contains(term)));
        }

        query = sort switch
        {
            "request" => sortDir == "desc"
                ? query.OrderByDescending(b => b.RequestUid)
                : query.OrderBy(b => b.RequestUid),
            "provider" => sortDir == "desc"
                ? query.OrderByDescending(b => b.ProviderU.UserU.FullName)
                : query.OrderBy(b => b.ProviderU.UserU.FullName),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(b => b.Status)
                : query.OrderBy(b => b.Status),
            "amount" => sortDir == "desc"
                ? query.OrderByDescending(b => b.FinalAmount)
                : query.OrderBy(b => b.FinalAmount),
            _ => sortDir == "desc"
                ? query.OrderByDescending(b => b.BookingDate)
                : query.OrderBy(b => b.BookingDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingItemVm
            {
                Uid = b.Uid,
                RequestLabel = $"#{b.RequestUid} - {b.RequestU.CustomerU.FullName}",
                ProviderName = b.ProviderU.UserU.FullName,
                BookingDate = b.BookingDate,
                FinalAmount = b.FinalAmount,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);

        return new BookingListVm
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

    public async Task<BookingDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _bookingRepo.Query()
            .Where(b => b.Uid == id)
            .Select(b => new BookingDetailsVm
            {
                Uid = b.Uid,
                RequestUid = b.RequestUid,
                RequestLabel = $"#{b.RequestUid} - {b.RequestU.CustomerU.FullName} ({b.RequestU.CategoryU.CategoryName})",
                ProviderUid = b.ProviderUid,
                ProviderName = b.ProviderU.UserU.FullName,
                BookingDate = b.BookingDate,
                FinalAmount = b.FinalAmount,
                Status = b.Status,
                TrackingCount = b.BookingTrackings.Count,
                PaymentCount = b.Payments.Count,
                ReviewCount = b.Reviews.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BookingFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _bookingRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return await PopulateFormAsync(new BookingFormVm
        {
            Uid = entity.Uid,
            RequestUid = entity.RequestUid,
            ProviderUid = entity.ProviderUid,
            FinalAmount = entity.FinalAmount,
            Status = entity.Status ?? "Accepted"
        }, cancellationToken);
    }

    public async Task<BookingDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _bookingRepo.Query()
            .Where(b => b.Uid == id)
            .Select(b => new BookingDeleteVm
            {
                Uid = b.Uid,
                RequestLabel = $"#{b.RequestUid} - {b.RequestU.CustomerU.FullName}",
                ProviderName = b.ProviderU.UserU.FullName,
                BookingDate = b.BookingDate,
                Status = b.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        BookingFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        var entity = new Booking
        {
            RequestUid = model.RequestUid,
            ProviderUid = model.ProviderUid,
            FinalAmount = model.FinalAmount,
            Status = model.Status,
            BookingDate = DateTime.Now
        };

        await _bookingRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        BookingFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _bookingRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null) return (false, "Booking not found.");

        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        entity.RequestUid = model.RequestUid;
        entity.ProviderUid = model.ProviderUid;
        entity.FinalAmount = model.FinalAmount;
        entity.Status = model.Status;

        await _bookingRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _bookingRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return (false, "Booking not found.");

        try
        {
            await _bookingRepo.DeleteAsync(entity, cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this booking because it has linked tracking, payments, or reviews.");
        }
    }

    public async Task<BookingFormVm> PopulateFormAsync(
        BookingFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Requests = await GetRequestOptionsAsync(cancellationToken);
        model.Providers = await GetProviderOptionsAsync(cancellationToken);
        model.StatusOptions = GetStatusOptions();
        return model;
    }

    private async Task<string?> ValidateAsync(BookingFormVm model, CancellationToken cancellationToken)
    {
        var requestExists = await _requestRepo.Query()
            .AnyAsync(r => r.Uid == model.RequestUid, cancellationToken);
        if (!requestExists) return "Selected service request does not exist.";

        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (!providerExists) return "Selected provider does not exist.";

        if (!ValidStatuses.Contains(model.Status)) return "Invalid status value.";

        return null;
    }
}
