using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ServiceRequestService : IServiceRequestService
{
    private static readonly string[] ValidStatuses = ["Pending", "Assigned", "In Progress", "Completed", "Cancelled"];

    private readonly AppDbContext _db;

    public ServiceRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SelectListItem>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Clients
            .AsNoTracking()
            .OrderBy(c => c.FullName)
            .Select(c => new SelectListItem
            {
                Value = c.Uid.ToString(),
                Text = c.FullName + " (" + c.User.MobileNo + ")"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SelectListItem>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
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

        var query = _db.CustomerServiceRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.Client.FullName.Contains(term) ||
                r.Category.CategoryName.Contains(term) ||
                r.ServiceTitle.Contains(term) ||
                r.ServiceDescription.Contains(term) ||
                r.ContactNo.Contains(term) ||
                r.Status.Contains(term) ||
                r.ClientAddress.FullAddress.Contains(term));
        }

        query = sort switch
        {
            "customer" => sortDir == "desc"
                ? query.OrderByDescending(r => r.Client.FullName)
                : query.OrderBy(r => r.Client.FullName),
            "category" => sortDir == "desc"
                ? query.OrderByDescending(r => r.Category.CategoryName)
                : query.OrderBy(r => r.Category.CategoryName),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(r => r.Status)
                : query.OrderBy(r => r.Status),
            _ => sortDir == "desc"
                ? query.OrderByDescending(r => r.CreatedOn)
                : query.OrderBy(r => r.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ServiceRequestItemVm
            {
                Uid = r.Uid,
                CustomerName = r.Client.FullName,
                CategoryName = r.Category.CategoryName,
                ServiceAddress = r.ClientAddress.AddressTitle + " - " + r.ClientAddress.FullAddress,
                ServiceTitle = r.ServiceTitle,
                Status = r.Status,
                IsUrgent = r.IsUrgent,
                RequestDate = r.CreatedOn
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
        var details = await _db.CustomerServiceRequests
            .AsNoTracking()
            .Where(r => r.Uid == id)
            .Select(r => new ServiceRequestDetailsVm
            {
                Uid = r.Uid,
                CustomerUid = r.ClientUid,
                CustomerName = r.Client.FullName,
                CategoryUid = r.CategoryUid,
                CategoryName = r.Category.CategoryName,
                ClientAddressUid = r.ClientAddressUid,
                ServiceAddress = r.ClientAddress.AddressTitle + " - " + r.ClientAddress.FullAddress
                                 + ", " + r.ClientAddress.Area + ", " + r.ClientAddress.City,
                ServiceTitle = r.ServiceTitle,
                ServiceDescription = r.ServiceDescription,
                PreferredServiceDate = r.PreferredServiceDate,
                PreferredServiceTime = r.PreferredServiceTime,
                IsUrgent = r.IsUrgent,
                ContactPerson = r.ContactPerson,
                ContactNo = r.ContactNo,
                EstimatedBudget = r.EstimatedBudget,
                RequestDate = r.CreatedOn,
                Status = r.Status,
                Remarks = r.Remarks
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (details == null) return null;

        details.BookingUid = await _db.ServiceBookings
            .AsNoTracking()
            .Where(b => b.RequestUid == id)
            .Select(b => (int?)b.Uid)
            .FirstOrDefaultAsync(cancellationToken);

        return details;
    }

    public async Task<ServiceRequestFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.CustomerServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Uid == id, cancellationToken);

        if (entity == null) return null;

        return await PopulateFormAsync(new ServiceRequestFormVm
        {
            Uid = entity.Uid,
            CustomerUid = entity.ClientUid,
            CategoryUid = entity.CategoryUid,
            ClientAddressUid = entity.ClientAddressUid,
            ServiceTitle = entity.ServiceTitle,
            ServiceDescription = entity.ServiceDescription,
            PreferredServiceDate = entity.PreferredServiceDate,
            PreferredServiceTime = entity.PreferredServiceTime,
            IsUrgent = entity.IsUrgent,
            ContactPerson = entity.ContactPerson,
            ContactNo = entity.ContactNo,
            EstimatedBudget = entity.EstimatedBudget,
            Status = entity.Status,
            Remarks = entity.Remarks
        }, cancellationToken);
    }

    public async Task<ServiceRequestDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.CustomerServiceRequests
            .AsNoTracking()
            .Where(r => r.Uid == id)
            .Select(r => new ServiceRequestDeleteVm
            {
                Uid = r.Uid,
                CustomerName = r.Client.FullName,
                CategoryName = r.Category.CategoryName,
                ServiceTitle = r.ServiceTitle,
                ServiceAddress = r.ClientAddress.FullAddress,
                Status = r.Status,
                RequestDate = r.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
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

        _db.CustomerServiceRequests.Add(new CustomerServiceRequest
        {
            ClientUid = model.CustomerUid,
            CategoryUid = model.CategoryUid,
            ClientAddressUid = model.ClientAddressUid,
            ServiceTitle = model.ServiceTitle.Trim(),
            ServiceDescription = model.ServiceDescription.Trim(),
            PreferredServiceDate = model.PreferredServiceDate,
            PreferredServiceTime = model.PreferredServiceTime?.Trim(),
            IsUrgent = model.IsUrgent,
            ContactPerson = model.ContactPerson?.Trim(),
            ContactNo = model.ContactNo.Trim(),
            EstimatedBudget = model.EstimatedBudget,
            Status = model.Status,
            Remarks = model.Remarks?.Trim(),
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        ServiceRequestFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.CustomerServiceRequests
            .FirstOrDefaultAsync(r => r.Uid == model.Uid, cancellationToken);

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

        entity.ClientUid = model.CustomerUid;
        entity.CategoryUid = model.CategoryUid;
        entity.ClientAddressUid = model.ClientAddressUid;
        entity.ServiceTitle = model.ServiceTitle.Trim();
        entity.ServiceDescription = model.ServiceDescription.Trim();
        entity.PreferredServiceDate = model.PreferredServiceDate;
        entity.PreferredServiceTime = model.PreferredServiceTime?.Trim();
        entity.IsUrgent = model.IsUrgent;
        entity.ContactPerson = model.ContactPerson?.Trim();
        entity.ContactNo = model.ContactNo.Trim();
        entity.EstimatedBudget = model.EstimatedBudget;
        entity.Status = model.Status;
        entity.Remarks = model.Remarks?.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.CustomerServiceRequests
            .FirstOrDefaultAsync(r => r.Uid == id, cancellationToken);

        if (entity == null)
        {
            return (false, "Service request not found.");
        }

        _db.CustomerServiceRequests.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<ServiceRequestFormVm> PopulateFormAsync(
        ServiceRequestFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Customers = await GetCustomerOptionsAsync(cancellationToken);
        model.Categories = await GetCategoryOptionsAsync(cancellationToken);
        model.StatusOptions = GetStatusOptions();
        model.Addresses = await GetAddressOptionsAsync(model.CustomerUid, cancellationToken);
        return model;
    }

    public async Task<List<SelectListItem>> GetAddressOptionsAsync(int clientUid, CancellationToken cancellationToken = default)
    {
        if (clientUid <= 0)
        {
            return new List<SelectListItem>();
        }

        return await _db.ClientAddresses
            .AsNoTracking()
            .Where(a => a.ClientUid == clientUid)
            .OrderBy(a => a.AddressTitle)
            .Select(a => new SelectListItem
            {
                Value = a.Uid.ToString(),
                Text = a.AddressTitle + " - " + a.FullAddress
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> ValidateForeignKeysAsync(
        ServiceRequestFormVm model,
        CancellationToken cancellationToken)
    {
        var clientExists = await _db.Clients
            .AnyAsync(c => c.Uid == model.CustomerUid, cancellationToken);

        if (!clientExists)
        {
            return "Selected client does not exist.";
        }

        var categoryExists = await _db.ServiceCategories
            .AnyAsync(c => c.Uid == model.CategoryUid && c.IsActive, cancellationToken);

        if (!categoryExists)
        {
            return "Selected category does not exist or is inactive.";
        }

        var addressValid = await _db.ClientAddresses
            .AnyAsync(a => a.Uid == model.ClientAddressUid && a.ClientUid == model.CustomerUid, cancellationToken);

        if (!addressValid)
        {
            return "Selected address does not belong to this client.";
        }

        return null;
    }
}
