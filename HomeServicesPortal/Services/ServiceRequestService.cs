using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ServiceRequestService : IServiceRequestService
{
    private static readonly string[] ValidStatuses = ["Pending", "Assigned", "In Progress", "Completed", "Cancelled"];

    private readonly IRepository<ServiceRequest> _requestRepo;
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<ServiceCategory> _categoryRepo;

    public ServiceRequestService(
        IRepository<ServiceRequest> requestRepo,
        IRepository<Customer> customerRepo,
        IRepository<ServiceCategory> categoryRepo)
    {
        _requestRepo = requestRepo;
        _customerRepo = customerRepo;
        _categoryRepo = categoryRepo;
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

    public async Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _categoryRepo.Query()
            .Where(c => c.IsActive != false)
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem
            {
                Value = c.Uid.ToString(),
                Text = c.CategoryName
            })
            .ToListAsync(cancellationToken);
    }

    public List<SelectListItem> GetStatusOptions()
    {
        return ValidStatuses
            .Select(s => new SelectListItem { Value = s, Text = s })
            .ToList();
    }

    public async Task<ServiceRequestListVm> GetListAsync(
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

        var query = _requestRepo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.CustomerU.FullName.Contains(term) ||
                r.CategoryU.CategoryName.Contains(term) ||
                (r.ServiceAddress != null && r.ServiceAddress.Contains(term)) ||
                (r.ProblemDescription != null && r.ProblemDescription.Contains(term)) ||
                (r.Status != null && r.Status.Contains(term)));
        }

        query = sort switch
        {
            "customer" => sortDir == "desc"
                ? query.OrderByDescending(r => r.CustomerU.FullName)
                : query.OrderBy(r => r.CustomerU.FullName),
            "category" => sortDir == "desc"
                ? query.OrderByDescending(r => r.CategoryU.CategoryName)
                : query.OrderBy(r => r.CategoryU.CategoryName),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(r => r.Status)
                : query.OrderBy(r => r.Status),
            _ => sortDir == "desc"
                ? query.OrderByDescending(r => r.RequestDate)
                : query.OrderBy(r => r.RequestDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ServiceRequestItemVm
            {
                Uid = r.Uid,
                CustomerName = r.CustomerU.FullName,
                CategoryName = r.CategoryU.CategoryName,
                ServiceAddress = r.ServiceAddress,
                Status = r.Status,
                RequestDate = r.RequestDate
            })
            .ToListAsync(cancellationToken);

        return new ServiceRequestListVm
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

    public async Task<ServiceRequestDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await _requestRepo.Query()
            .FirstOrDefaultAsync(r => r.Uid == id, cancellationToken);

        if (request == null) return null;

        var customerName = await _customerRepo.Query()
            .Where(c => c.Uid == request.CustomerUid)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        var categoryName = await _categoryRepo.Query()
            .Where(c => c.Uid == request.CategoryUid)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        var quoteCount = await _requestRepo.Query()
            .Where(r => r.Uid == id)
            .Select(r => r.ProviderQuotes.Count)
            .FirstOrDefaultAsync(cancellationToken);

        var bookingCount = await _requestRepo.Query()
            .Where(r => r.Uid == id)
            .Select(r => r.Bookings.Count)
            .FirstOrDefaultAsync(cancellationToken);

        return new ServiceRequestDetailsVm
        {
            Uid = request.Uid,
            CustomerUid = request.CustomerUid,
            CustomerName = customerName,
            CategoryUid = request.CategoryUid,
            CategoryName = categoryName,
            ServiceAddress = request.ServiceAddress,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ProblemDescription = request.ProblemDescription,
            RequestDate = request.RequestDate,
            Status = request.Status,
            QuoteCount = quoteCount,
            BookingCount = bookingCount
        };
    }

    public async Task<ServiceRequestFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _requestRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return await PopulateFormAsync(new ServiceRequestFormVm
        {
            Uid = entity.Uid,
            CustomerUid = entity.CustomerUid,
            CategoryUid = entity.CategoryUid,
            ServiceAddress = entity.ServiceAddress,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            ProblemDescription = entity.ProblemDescription,
            Status = entity.Status ?? "Pending"
        }, cancellationToken);
    }

    public async Task<ServiceRequestDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var request = await _requestRepo.Query()
            .FirstOrDefaultAsync(r => r.Uid == id, cancellationToken);

        if (request == null) return null;

        var customerName = await _customerRepo.Query()
            .Where(c => c.Uid == request.CustomerUid)
            .Select(c => c.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        var categoryName = await _categoryRepo.Query()
            .Where(c => c.Uid == request.CategoryUid)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync(cancellationToken) ?? "—";

        return new ServiceRequestDeleteVm
        {
            Uid = request.Uid,
            CustomerName = customerName,
            CategoryName = categoryName,
            ServiceAddress = request.ServiceAddress,
            Status = request.Status,
            RequestDate = request.RequestDate
        };
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        ServiceRequestFormVm model,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateForeignKeysAsync(model, cancellationToken);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (!ValidStatuses.Contains(model.Status))
        {
            return (false, "Invalid status value.");
        }

        var entity = new ServiceRequest
        {
            CustomerUid = model.CustomerUid,
            CategoryUid = model.CategoryUid,
            ServiceAddress = model.ServiceAddress?.Trim(),
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            ProblemDescription = model.ProblemDescription?.Trim(),
            RequestDate = DateTime.Now,
            Status = model.Status
        };

        await _requestRepo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceRequestFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _requestRepo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null)
        {
            return (false, "Service request not found.");
        }

        var validationError = await ValidateForeignKeysAsync(model, cancellationToken);
        if (validationError != null)
        {
            return (false, validationError);
        }

        if (!ValidStatuses.Contains(model.Status))
        {
            return (false, "Invalid status value.");
        }

        entity.CustomerUid = model.CustomerUid;
        entity.CategoryUid = model.CategoryUid;
        entity.ServiceAddress = model.ServiceAddress?.Trim();
        entity.Latitude = model.Latitude;
        entity.Longitude = model.Longitude;
        entity.ProblemDescription = model.ProblemDescription?.Trim();
        entity.Status = model.Status;

        await _requestRepo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _requestRepo.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return (false, "Service request not found.");
        }

        try
        {
            await _requestRepo.DeleteAsync(entity, cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this request because it has linked quotes or bookings.");
        }
    }

    public async Task<ServiceRequestFormVm> PopulateFormAsync(
        ServiceRequestFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Customers = await GetCustomerOptionsAsync(cancellationToken);
        model.Categories = await GetCategoryOptionsAsync(cancellationToken);
        model.StatusOptions = GetStatusOptions();
        return model;
    }

    private async Task<string?> ValidateForeignKeysAsync(
        ServiceRequestFormVm model,
        CancellationToken cancellationToken)
    {
        var customerExists = await _customerRepo.Query()
            .AnyAsync(c => c.Uid == model.CustomerUid, cancellationToken);

        if (!customerExists)
        {
            return "Selected customer does not exist.";
        }

        var categoryExists = await _categoryRepo.Query()
            .AnyAsync(c => c.Uid == model.CategoryUid, cancellationToken);

        if (!categoryExists)
        {
            return "Selected category does not exist.";
        }

        return null;
    }
}
