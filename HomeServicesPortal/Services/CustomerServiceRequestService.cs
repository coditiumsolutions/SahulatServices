using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
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

        var results = await query
            .OrderByDescending(r => r.CreatedOn)
            .ThenByDescending(r => r.Uid)
            .Select(MapToProgressInputExpression())
            .ToListAsync(cancellationToken);

        foreach (var result in results)
        {
            ApplyProgressStatus(result.Dto, result.BookingStatus);
        }

        return results.Select(r => r.Dto).ToList();
    }

    public async Task<(bool Success, string? Error, CustomerServiceRequestApiDto? Data)> GetRequestByIdAsync(
        int requestUid,
        CancellationToken cancellationToken = default)
    {
        var result = await _db.CustomerServiceRequests
            .AsNoTracking()
            .Where(r => r.Uid == requestUid)
            .Select(MapToProgressInputExpression())
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return (false, "Service request not found.", null);
        }

        ApplyProgressStatus(result.Dto, result.BookingStatus);

        return (true, null, result.Dto);
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

        if (!RequestStatusConstants.ClientEditableStatuses.Contains(request.Status.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Invalid status value.", null);
        }

        if (string.Equals(request.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.CancelReason))
        {
            return (false, "Cancel reason is required when cancelling a service request.", null);
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
        entity.CancelReason = string.Equals(request.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            ? request.CancelReason?.Trim()
            : entity.CancelReason;

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

    private static readonly string[] ProviderVisibleStatuses = ["Accepted", "In Progress", "Completed", "Closed"];

    private record ProgressInput(CustomerServiceRequestApiDto Dto, string? BookingStatus);

    /// <summary>
    /// Computes the client-facing progress-bar stage from the request's own status and its
    /// linked (non-Rejected) booking's status, per docs/status-workflow.md. Cancelled always
    /// wins and suppresses the field entirely (null) rather than appearing as a stage.
    /// </summary>
    private static void ApplyProgressStatus(CustomerServiceRequestApiDto dto, string? bookingStatus)
    {
        if (string.Equals(dto.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(bookingStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            dto.ProgressStatus = null;
            return;
        }

        if (bookingStatus == null)
        {
            dto.ProgressStatus = "Requested";
            return;
        }

        if (string.Equals(bookingStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(bookingStatus, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            dto.ProgressStatus = "Completed";
            return;
        }

        if (string.Equals(bookingStatus, "In Progress", StringComparison.OrdinalIgnoreCase))
        {
            dto.ProgressStatus = "In Progress";
            return;
        }

        if (string.Equals(bookingStatus, "Accepted", StringComparison.OrdinalIgnoreCase)
            && HasScheduleTimeArrived(dto.PreferredServiceDate, dto.PreferredServiceTime))
        {
            dto.ProgressStatus = "In Progress";
            return;
        }

        // Booking is "Pending" (awaiting provider response) or "Accepted" but not yet due.
        dto.ProgressStatus = "Assigned";
    }

    private static bool HasScheduleTimeArrived(DateOnly? preferredDate, string? preferredTime)
    {
        if (preferredDate == null)
        {
            return false;
        }

        var time = TimeOnly.MinValue;
        if (!string.IsNullOrWhiteSpace(preferredTime))
        {
            TimeOnly.TryParse(preferredTime, out time);
        }

        return DateTime.Now >= preferredDate.Value.ToDateTime(time);
    }

    private System.Linq.Expressions.Expression<Func<CustomerServiceRequest, ProgressInput>> MapToProgressInputExpression() =>
        r => new ProgressInput(
            new CustomerServiceRequestApiDto
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
            CancelReason = r.CancelReason,
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
            },
            _db.ServiceBookings
                .Where(b => b.RequestUid == r.Uid && b.Status != "Rejected")
                .OrderByDescending(b => b.Uid)
                .Select(b => b.Status)
                .FirstOrDefault());
}
