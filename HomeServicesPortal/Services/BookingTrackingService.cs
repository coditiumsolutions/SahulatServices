using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class BookingTrackingService : IBookingTrackingService
{
    private static readonly string[] ValidStatuses =
        ["On The Way", "Arrived", "In Progress", "Completed", "Cancelled"];

    private readonly IRepository<BookingTracking> _trackingRepo;
    private readonly IRepository<Booking> _bookingRepo;

    public BookingTrackingService(
        IRepository<BookingTracking> trackingRepo,
        IRepository<Booking> bookingRepo)
    {
        _trackingRepo = trackingRepo;
        _bookingRepo = bookingRepo;
    }

    public async Task<List<SelectListItem>> GetBookingOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _bookingRepo.Query()
            .OrderByDescending(b => b.BookingDate)
            .Select(b => new SelectListItem
            {
                Value = b.Uid.ToString(),
                Text = $"#{b.Uid} - {b.ProviderU.FullName} / Req #{b.RequestUid}"
            })
            .ToListAsync(cancellationToken);
    }

    public List<SelectListItem> GetStatusOptions()
    {
        return ValidStatuses.Select(s => new SelectListItem { Value = s, Text = s }).ToList();
    }

    public async Task<BookingTrackingListVm> GetListAsync(
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

        var query = _trackingRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(t =>
                t.Bookin.ProviderU.FullName.Contains(term) ||
                (t.Status != null && t.Status.Contains(term)) ||
                (t.Remarks != null && t.Remarks.Contains(term)));
        }

        query = sort switch
        {
            "booking" => sortDir == "desc"
                ? query.OrderByDescending(t => t.BookingUid)
                : query.OrderBy(t => t.BookingUid),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            _ => sortDir == "desc"
                ? query.OrderByDescending(t => t.StatusDate)
                : query.OrderBy(t => t.StatusDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new BookingTrackingItemVm
            {
                Uid = t.Uid,
                BookingLabel = $"#{t.BookingUid} - {t.Bookin.ProviderU.FullName}",
                Status = t.Status,
                Remarks = t.Remarks,
                StatusDate = t.StatusDate
            })
            .ToListAsync(cancellationToken);

        return new BookingTrackingListVm
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

    public async Task<BookingTrackingDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _trackingRepo.Query()
            .Where(t => t.Uid == id)
            .Select(t => new BookingTrackingDetailsVm
            {
                Uid = t.Uid,
                BookingUid = t.BookingUid,
                BookingLabel = $"#{t.BookingUid} - {t.Bookin.ProviderU.FullName} / Req #{t.Bookin.RequestUid}",
                Status = t.Status,
                Remarks = t.Remarks,
                StatusDate = t.StatusDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BookingTrackingFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _trackingRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return await PopulateFormAsync(new BookingTrackingFormVm
        {
            Uid = entity.Uid,
            BookingUid = entity.BookingUid,
            Status = entity.Status,
            Remarks = entity.Remarks
        }, cancellationToken);
    }

    public async Task<BookingTrackingDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _trackingRepo.Query()
            .Where(t => t.Uid == id)
            .Select(t => new BookingTrackingDeleteVm
            {
                Uid = t.Uid,
                BookingLabel = $"#{t.BookingUid} - {t.Bookin.ProviderU.FullName}",
                Status = t.Status,
                StatusDate = t.StatusDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        BookingTrackingFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        var entity = new BookingTracking
        {
            BookingUid = model.BookingUid,
            Status = model.Status?.Trim(),
            Remarks = model.Remarks?.Trim(),
            StatusDate = DateTime.Now
        };

        await _trackingRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        BookingTrackingFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _trackingRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null) return (false, "Booking tracking record not found.");

        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        entity.BookingUid = model.BookingUid;
        entity.Status = model.Status?.Trim();
        entity.Remarks = model.Remarks?.Trim();

        await _trackingRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _trackingRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return (false, "Booking tracking record not found.");

        await _trackingRepo.DeleteAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<BookingTrackingFormVm> PopulateFormAsync(
        BookingTrackingFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Bookings = await GetBookingOptionsAsync(cancellationToken);
        model.StatusOptions = GetStatusOptions();
        return model;
    }

    private async Task<string?> ValidateAsync(
        BookingTrackingFormVm model,
        CancellationToken cancellationToken)
    {
        var bookingExists = await _bookingRepo.Query()
            .AnyAsync(b => b.Uid == model.BookingUid, cancellationToken);
        if (!bookingExists) return "Selected booking does not exist.";

        if (!string.IsNullOrWhiteSpace(model.Status) && !ValidStatuses.Contains(model.Status))
        {
            return "Invalid status value.";
        }

        return null;
    }
}
