using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using ServiceProviderEntity = HomeServicesPortal.Models.Entities.ServiceProvider;

namespace HomeServicesPortal.Services;

public class ReviewService : IReviewService
{
    private readonly IRepository<Review> _reviewRepo;
    private readonly IRepository<Booking> _bookingRepo;
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<ServiceProviderEntity> _providerRepo;

    public ReviewService(
        IRepository<Review> reviewRepo,
        IRepository<Booking> bookingRepo,
        IRepository<Customer> customerRepo,
        IRepository<ServiceProviderEntity> providerRepo)
    {
        _reviewRepo = reviewRepo;
        _bookingRepo = bookingRepo;
        _customerRepo = customerRepo;
        _providerRepo = providerRepo;
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

    public async Task<List<SelectListItem>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _customerRepo.Query()
            .OrderBy(c => c.FullName)
            .Select(c => new SelectListItem
            {
                Value = c.Uid.ToString(),
                Text = c.FullName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _providerRepo.Query()
            .Where(p => p.IsActive != false)
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.FullName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewListVm> GetListAsync(
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

        var query = _reviewRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.CustomerU.FullName.Contains(term) ||
                r.ProviderU.FullName.Contains(term) ||
                (r.ReviewText != null && r.ReviewText.Contains(term)));
        }

        query = sort switch
        {
            "customer" => sortDir == "desc"
                ? query.OrderByDescending(r => r.CustomerU.FullName)
                : query.OrderBy(r => r.CustomerU.FullName),
            "provider" => sortDir == "desc"
                ? query.OrderByDescending(r => r.ProviderU.FullName)
                : query.OrderBy(r => r.ProviderU.FullName),
            "rating" => sortDir == "desc"
                ? query.OrderByDescending(r => r.Rating)
                : query.OrderBy(r => r.Rating),
            "booking" => sortDir == "desc"
                ? query.OrderByDescending(r => r.BookingUid)
                : query.OrderBy(r => r.BookingUid),
            _ => sortDir == "desc"
                ? query.OrderByDescending(r => r.ReviewDate)
                : query.OrderBy(r => r.ReviewDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewItemVm
            {
                Uid = r.Uid,
                BookingLabel = $"#{r.BookingUid} - {r.Bookin.ProviderU.FullName}",
                CustomerName = r.CustomerU.FullName,
                ProviderName = r.ProviderU.FullName,
                Rating = r.Rating,
                ReviewDate = r.ReviewDate
            })
            .ToListAsync(cancellationToken);

        return new ReviewListVm
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

    public async Task<ReviewDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _reviewRepo.Query()
            .Where(r => r.Uid == id)
            .Select(r => new ReviewDetailsVm
            {
                Uid = r.Uid,
                BookingUid = r.BookingUid,
                BookingLabel = $"#{r.BookingUid} - {r.Bookin.ProviderU.FullName} / Req #{r.Bookin.RequestUid}",
                CustomerUid = r.CustomerUid,
                CustomerName = r.CustomerU.FullName,
                ProviderUid = r.ProviderUid,
                ProviderName = r.ProviderU.FullName,
                Rating = r.Rating,
                ReviewText = r.ReviewText,
                ReviewDate = r.ReviewDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ReviewFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _reviewRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return await PopulateFormAsync(new ReviewFormVm
        {
            Uid = entity.Uid,
            BookingUid = entity.BookingUid,
            CustomerUid = entity.CustomerUid,
            ProviderUid = entity.ProviderUid,
            Rating = entity.Rating,
            ReviewText = entity.ReviewText
        }, cancellationToken);
    }

    public async Task<ReviewDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _reviewRepo.Query()
            .Where(r => r.Uid == id)
            .Select(r => new ReviewDeleteVm
            {
                Uid = r.Uid,
                BookingLabel = $"#{r.BookingUid} - {r.Bookin.ProviderU.FullName}",
                CustomerName = r.CustomerU.FullName,
                ProviderName = r.ProviderU.FullName,
                Rating = r.Rating,
                ReviewDate = r.ReviewDate
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ReviewFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        var entity = new Review
        {
            BookingUid = model.BookingUid,
            CustomerUid = model.CustomerUid,
            ProviderUid = model.ProviderUid,
            Rating = model.Rating,
            ReviewText = model.ReviewText?.Trim(),
            ReviewDate = DateTime.Now
        };

        await _reviewRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ReviewFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _reviewRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null) return (false, "Review not found.");

        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        entity.BookingUid = model.BookingUid;
        entity.CustomerUid = model.CustomerUid;
        entity.ProviderUid = model.ProviderUid;
        entity.Rating = model.Rating;
        entity.ReviewText = model.ReviewText?.Trim();

        await _reviewRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _reviewRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return (false, "Review not found.");

        await _reviewRepo.DeleteAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<ReviewFormVm> PopulateFormAsync(
        ReviewFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Bookings = await GetBookingOptionsAsync(cancellationToken);
        model.Customers = await GetCustomerOptionsAsync(cancellationToken);
        model.Providers = await GetProviderOptionsAsync(cancellationToken);
        return model;
    }

    private async Task<string?> ValidateAsync(ReviewFormVm model, CancellationToken cancellationToken)
    {
        var bookingExists = await _bookingRepo.Query()
            .AnyAsync(b => b.Uid == model.BookingUid, cancellationToken);
        if (!bookingExists) return "Selected booking does not exist.";

        var customerExists = await _customerRepo.Query()
            .AnyAsync(c => c.Uid == model.CustomerUid, cancellationToken);
        if (!customerExists) return "Selected customer does not exist.";

        var providerExists = await _providerRepo.Query()
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (!providerExists) return "Selected provider does not exist.";

        if (model.Rating is < 1 or > 5) return "Rating must be between 1 and 5.";

        return null;
    }
}
