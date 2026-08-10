using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class CustomerServiceRequestService : ICustomerServiceRequestService
{
    private readonly AppDbContext _db;

    public CustomerServiceRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerServiceRequestApiDto>> GetRequestsAsync(
        int? clientUid,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CustomerServiceRequests.AsNoTracking();

        if (clientUid.HasValue)
        {
            query = query.Where(r => r.ClientUid == clientUid.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedOn)
            .ThenByDescending(r => r.Uid)
            .Select(MapToDtoExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error, CustomerServiceRequestApiDto? Data)> GetRequestByIdAsync(
        int requestUid,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.CustomerServiceRequests
            .AsNoTracking()
            .Where(r => r.Uid == requestUid)
            .Select(MapToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        if (request == null)
        {
            return (false, "Service request not found.", null);
        }

        return (true, null, request);
    }

    public async Task<(bool Success, string? Error, CustomerServiceRequestApiDto? Data)> CreateRequestAsync(
        CreateCustomerServiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateReferencesAsync(
            request.ClientUid,
            request.CategoryUid,
            request.ClientAddressUid,
            cancellationToken);

        if (validationError != null)
        {
            return (false, validationError, null);
        }

        var entity = new CustomerServiceRequest
        {
            ClientUid = request.ClientUid,
            CategoryUid = request.CategoryUid,
            ClientAddressUid = request.ClientAddressUid,
            ServiceTitle = request.ServiceTitle.Trim(),
            ServiceDescription = NormalizeOptionalText(request.ServiceDescription),
            PreferredServiceDate = request.PreferredServiceDate,
            PreferredServiceTime = NormalizeOptionalText(request.PreferredServiceTime),
            IsUrgent = request.IsUrgent,
            ContactPerson = request.ContactPerson?.Trim(),
            ContactNo = request.ContactNo.Trim(),
            EstimatedBudget = request.EstimatedBudget,
            Status = "Pending",
            Remarks = request.Remarks?.Trim(),
            CreatedOn = DateTime.Now
        };

        _db.CustomerServiceRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetRequestByIdAsync(entity.Uid, cancellationToken);
    }

    public async Task<(bool Success, string? Error, CustomerServiceRequestApiDto? Data)> UpdateRequestAsync(
        UpdateCustomerServiceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.CustomerServiceRequests
            .FirstOrDefaultAsync(r => r.Uid == request.RequestUid, cancellationToken);

        if (entity == null)
        {
            return (false, "Service request not found.", null);
        }

        var validationError = await ValidateReferencesAsync(
            entity.ClientUid,
            request.CategoryUid,
            request.ClientAddressUid,
            cancellationToken);

        if (validationError != null)
        {
            return (false, validationError, null);
        }

        entity.CategoryUid = request.CategoryUid;
        entity.ClientAddressUid = request.ClientAddressUid;
        entity.ServiceTitle = request.ServiceTitle.Trim();
        entity.ServiceDescription = NormalizeOptionalText(request.ServiceDescription);
        entity.PreferredServiceDate = request.PreferredServiceDate;
        entity.PreferredServiceTime = NormalizeOptionalText(request.PreferredServiceTime);
        entity.IsUrgent = request.IsUrgent;
        entity.ContactPerson = request.ContactPerson?.Trim();
        entity.ContactNo = request.ContactNo.Trim();
        entity.EstimatedBudget = request.EstimatedBudget;
        entity.Status = request.Status.Trim();
        entity.Remarks = request.Remarks?.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return await GetRequestByIdAsync(entity.Uid, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> DeleteRequestAsync(
        int requestUid,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.CustomerServiceRequests
            .FirstOrDefaultAsync(r => r.Uid == requestUid, cancellationToken);

        if (entity == null)
        {
            return (false, "Service request not found.");
        }

        _db.CustomerServiceRequests.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private async Task<string?> ValidateReferencesAsync(
        int clientUid,
        int categoryUid,
        int clientAddressUid,
        CancellationToken cancellationToken)
    {
        var clientExists = await _db.Clients
            .AsNoTracking()
            .AnyAsync(c => c.Uid == clientUid, cancellationToken);

        if (!clientExists)
        {
            return "Client not found.";
        }

        var categoryExists = await _db.ServiceCategories
            .AsNoTracking()
            .AnyAsync(c => c.Uid == categoryUid && c.IsActive, cancellationToken);

        if (!categoryExists)
        {
            return "Invalid or inactive service category.";
        }

        var addressValid = await _db.ClientAddresses
            .AsNoTracking()
            .AnyAsync(a => a.Uid == clientAddressUid && a.ClientUid == clientUid, cancellationToken);

        if (!addressValid)
        {
            return "Client address not found for this client.";
        }

        return null;
    }

    private static readonly string[] ProviderVisibleStatuses = ["Accepted", "In Progress", "Completed"];

    private System.Linq.Expressions.Expression<Func<CustomerServiceRequest, CustomerServiceRequestApiDto>> MapToDtoExpression() =>
        r => new CustomerServiceRequestApiDto
        {
            Uid = r.Uid,
            ClientUid = r.ClientUid,
            ClientName = r.Client.FullName,
            CategoryUid = r.CategoryUid,
            CategoryName = r.Category.CategoryName,
            ClientAddressUid = r.ClientAddressUid,
            AddressTitle = r.ClientAddress.AddressTitle,
            ServiceTitle = r.ServiceTitle,
            ServiceDescription = r.ServiceDescription,
            PreferredServiceDate = r.PreferredServiceDate,
            PreferredServiceTime = r.PreferredServiceTime,
            IsUrgent = r.IsUrgent,
            ContactPerson = r.ContactPerson,
            ContactNo = r.ContactNo,
            EstimatedBudget = r.EstimatedBudget,
            Status = r.Status,
            Remarks = r.Remarks,
            CreatedOn = r.CreatedOn,
            ProviderUid = _db.ServiceBookings
                .Where(b => b.RequestUid == r.Uid && ProviderVisibleStatuses.Contains(b.Status))
                .Select(b => (int?)b.ProviderUid)
                .FirstOrDefault(),
            ProviderName = _db.ServiceBookings
                .Where(b => b.RequestUid == r.Uid && ProviderVisibleStatuses.Contains(b.Status))
                .Select(b => b.Provider.FullName)
                .FirstOrDefault(),
            ProviderMobileNo = _db.ServiceBookings
                .Where(b => b.RequestUid == r.Uid && ProviderVisibleStatuses.Contains(b.Status))
                .Select(b => b.Provider.User.MobileNo)
                .FirstOrDefault(),
            ProviderProfilePhotoPath = _db.ServiceBookings
                .Where(b => b.RequestUid == r.Uid && ProviderVisibleStatuses.Contains(b.Status))
                .Select(b => _db.ProviderDocuments
                    .Where(d => d.ProviderUid == b.ProviderUid)
                    .Select(d => d.ProfilePhotoPath)
                    .FirstOrDefault())
                .FirstOrDefault(),
            ProviderCnic = _db.ServiceBookings
                .Where(b => b.RequestUid == r.Uid && ProviderVisibleStatuses.Contains(b.Status))
                .Select(b => b.Provider.Cnic)
                .FirstOrDefault(),
            Passcode = _db.ServiceBookings
                .Where(b => b.RequestUid == r.Uid && b.Passcode != null)
                .Select(b => b.Passcode)
                .FirstOrDefault()
        };
}
