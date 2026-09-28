using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class BookingService : IBookingService
{
    private static readonly string[] ValidStatuses = ["Pending", "Accepted", "In Progress", "Completed", "Closed", "Cancelled", "Rejected"];
    private static readonly string[] ValidPaymentModes = ["CashToProvider", "OnlineToCompany"];
    private static readonly string[] ValidCommissionTypes = ["Percent", "Fixed"];

    private readonly AppDbContext _db;
    private readonly IPaymentService _payments;
    private readonly ICommissionRuleService _commissionRules;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        AppDbContext db,
        IPaymentService payments,
        ICommissionRuleService commissionRules,
        IServiceScopeFactory scopeFactory,
        ILogger<BookingService> logger)
    {
        _db = db;
        _payments = payments;
        _commissionRules = commissionRules;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<List<SelectListItem>> GetRequestOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CustomerServiceRequests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedOn)
            .Select(r => new SelectListItem
            {
                Value = r.Uid.ToString(),
            Text = r.ServiceTitle + " / " + r.Client.FullName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Providers
            .AsNoTracking()
            .Where(p => p.User.IsActive)
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.FullName + " (" + p.Category.CategoryName + ")"
            })
            .ToListAsync(cancellationToken);
    }

    public List<SelectListItem> GetStatusOptions() =>
        ValidStatuses.Select(s => new SelectListItem { Value = s, Text = s }).ToList();

    public async Task<BookingListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        string? status,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "date" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        var query = _db.ServiceBookings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                b.Client.FullName.Contains(term) ||
                b.Provider.FullName.Contains(term) ||
                b.Request.ServiceTitle.Contains(term) ||
                b.PaymentMode.Contains(term) ||
                b.Status.Contains(term));
        }

        query = sort switch
        {
            "request" => sortDir == "desc"
                ? query.OrderByDescending(b => b.RequestUid)
                : query.OrderBy(b => b.RequestUid),
            "provider" => sortDir == "desc"
                ? query.OrderByDescending(b => b.Provider.FullName)
                : query.OrderBy(b => b.Provider.FullName),
            "client" => sortDir == "desc"
                ? query.OrderByDescending(b => b.Client.FullName)
                : query.OrderBy(b => b.Client.FullName),
            "status" => sortDir == "desc"
                ? query.OrderByDescending(b => b.Status)
                : query.OrderBy(b => b.Status),
            "amount" => sortDir == "desc"
                ? query.OrderByDescending(b => b.FinalAmount)
                : query.OrderBy(b => b.FinalAmount),
            _ => sortDir == "desc"
                ? query.OrderByDescending(b => b.CreatedOn).ThenByDescending(b => b.Uid)
                : query.OrderBy(b => b.CreatedOn).ThenByDescending(b => b.Uid)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var statusCounts = await _db.ServiceBookings
            .AsNoTracking()
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingItemVm
            {
                Uid = b.Uid,
                RequestLabel = b.Request.ServiceTitle,
                ClientName = b.Client.FullName,
                ProviderName = b.Provider.FullName,
                BookingDate = b.CreatedOn,
                FinalAmount = b.FinalAmount,
                PaymentMode = b.PaymentMode,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);

        return new BookingListVm
        {
            Items = items,
            Search = search,
            Sort = sort,
            SortDir = sortDir,
            Status = status,
            StatusOptions = ValidStatuses,
            StatusCounts = statusCounts,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BookingDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var vm = await _db.ServiceBookings
            .AsNoTracking()
            .Where(b => b.Uid == id)
            .Select(b => new BookingDetailsVm
            {
                Uid = b.Uid,
                RequestUid = b.RequestUid,
                RequestLabel = b.Request.ServiceTitle,
                ServiceTitle = b.Request.ServiceTitle,
                ServiceType = b.Request.Category.CategoryName,
                ServiceDetail = b.ServiceDetail,
                ClientUid = b.ClientUid,
                ClientName = b.Client.FullName,
                ProviderUid = b.ProviderUid,
                ProviderName = b.Provider.FullName,
                BookingDate = b.CreatedOn,
                EstimatedAmount = b.EstimatedAmount,
                LabourAmount = b.LabourAmount,
                VisitCharges = b.VisitCharges,
                AdditionalCharges = b.AdditionalCharges,
                Deductions = b.Deductions,
                FinalAmount = b.FinalAmount,
                CustomerPaid = b.CustomerPaid,
                PaymentMode = b.PaymentMode,
                CustomerRemaining = b.CustomerRemaining,
                CommissionType = b.CommissionType,
                CommissionValue = b.CommissionValue,
                CommissionAmount = b.CommissionAmount,
                ProviderEarning = b.ProviderEarning,
                Status = b.Status,
                RejectReason = b.RejectReason,
                CancelReason = b.CancelReason,
                LedgerCount = _db.PaymentLedgers.Count(l => l.BookingUid == b.Uid)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (vm == null) return null;

        vm.MaterialItems = await _db.BookingMaterialItems
            .AsNoTracking()
            .Where(i => i.BookingUid == id)
            .OrderBy(i => i.Uid)
            .Select(i => new BookingMaterialItemInputVm
            {
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            })
            .ToListAsync(cancellationToken);

        return vm;
    }

    public async Task<BookingFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceBookings
            .AsNoTracking()
            .Where(b => b.Uid == id)
            .Select(b => new
            {
                Booking = b,
                ServiceTitle = b.Request.ServiceTitle,
                ClientName = b.Client.FullName,
                ServiceType = b.Request.Category.CategoryName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (entity == null) return null;

        var materialItems = await _db.BookingMaterialItems
            .AsNoTracking()
            .Where(i => i.BookingUid == id)
            .OrderBy(i => i.Uid)
            .Select(i => new BookingMaterialItemInputVm
            {
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            })
            .ToListAsync(cancellationToken);

        return await PopulateFormAsync(new BookingFormVm
        {
            Uid = entity.Booking.Uid,
            LockRequestFields = true,
            RequestUid = entity.Booking.RequestUid,
            ServiceTitle = entity.ServiceTitle,
            ClientName = entity.ClientName,
            ServiceType = entity.ServiceType,
            ServiceDetail = entity.Booking.ServiceDetail,
            ProviderUid = entity.Booking.ProviderUid,
            EstimatedAmount = entity.Booking.EstimatedAmount,
            LabourAmount = entity.Booking.LabourAmount,
            MaterialItems = materialItems,
            VisitCharges = entity.Booking.VisitCharges,
            AdditionalCharges = entity.Booking.AdditionalCharges,
            Deductions = entity.Booking.Deductions,
            FinalAmount = entity.Booking.FinalAmount,
            CustomerPaid = entity.Booking.CustomerPaid,
            PaymentMode = entity.Booking.PaymentMode,
            CustomerRemaining = entity.Booking.CustomerRemaining,
            CommissionType = entity.Booking.CommissionType,
            CommissionValue = entity.Booking.CommissionValue,
            CommissionAmount = entity.Booking.CommissionAmount,
            ProviderEarning = entity.Booking.ProviderEarning,
            Status = entity.Booking.Status
        }, cancellationToken);
    }

    public async Task<BookingDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ServiceBookings
            .AsNoTracking()
            .Where(b => b.Uid == id)
            .Select(b => new BookingDeleteVm
            {
                Uid = b.Uid,
                RequestLabel = b.Request.ServiceTitle,
                ClientName = b.Client.FullName,
                ProviderName = b.Provider.FullName,
                BookingDate = b.CreatedOn,
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

        var request = await _db.CustomerServiceRequests
            .AsNoTracking()
            .FirstAsync(r => r.Uid == model.RequestUid, cancellationToken);

        ApplyBookingTotals(model);
        var validationErrorAfterCalc = ValidateBookingTotals(model.FinalAmount, model.CommissionAmount, model.CustomerPaid);
        if (validationErrorAfterCalc != null) return (false, validationErrorAfterCalc);

        var booking = new ServiceBooking
        {
            RequestUid = model.RequestUid,
            ClientUid = request.ClientUid,
            ProviderUid = model.ProviderUid,
            ServiceDetail = string.IsNullOrWhiteSpace(model.ServiceDetail) ? null : model.ServiceDetail.Trim(),
            EstimatedAmount = model.EstimatedAmount,
            LabourAmount = model.LabourAmount,
            VisitCharges = model.VisitCharges,
            AdditionalCharges = model.AdditionalCharges,
            Deductions = model.Deductions,
            FinalAmount = model.FinalAmount,
            CustomerPaid = model.CustomerPaid,
            PaymentMode = model.PaymentMode.Trim(),
            CustomerRemaining = model.CustomerRemaining,
            CommissionType = model.CommissionType.Trim(),
            CommissionValue = model.CommissionValue,
            CommissionAmount = model.CommissionAmount ?? 0,
            ProviderEarning = model.ProviderEarning ?? 0,
            Status = model.Status.Trim(),
            CreatedOn = DateTime.Now
        };

        _db.ServiceBookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        await ReplaceMaterialItemsAsync(booking.Uid, model.MaterialItems, cancellationToken);

        if (string.Equals(booking.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            await _payments.RecordBookingCompletionAsync(booking, cancellationToken);
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        BookingFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceBookings
            .FirstOrDefaultAsync(b => b.Uid == model.Uid, cancellationToken);

        if (entity == null) return (false, "Booking not found.");

        // Preserve original request link on update.
        model.RequestUid = entity.RequestUid;

        var validationError = await ValidateAsync(model, cancellationToken);
        if (validationError != null) return (false, validationError);

        var isPostAcceptanceCancel = string.Equals(model.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(entity.Status, "Accepted", StringComparison.OrdinalIgnoreCase)
                || string.Equals(entity.Status, "In Progress", StringComparison.OrdinalIgnoreCase));

        if (isPostAcceptanceCancel && string.IsNullOrWhiteSpace(model.CancelReason))
        {
            return (false, "Cancel reason is required when cancelling an accepted booking.");
        }

        ApplyBookingTotals(model);
        var validationErrorAfterCalc = ValidateBookingTotals(model.FinalAmount, model.CommissionAmount, model.CustomerPaid);
        if (validationErrorAfterCalc != null) return (false, validationErrorAfterCalc);

        // Keep request linkage stable on edit.
        entity.ProviderUid = model.ProviderUid;
        entity.ServiceDetail = string.IsNullOrWhiteSpace(model.ServiceDetail) ? null : model.ServiceDetail.Trim();
        entity.EstimatedAmount = model.EstimatedAmount;
        entity.LabourAmount = model.LabourAmount;
        entity.VisitCharges = model.VisitCharges;
        entity.AdditionalCharges = model.AdditionalCharges;
        entity.Deductions = model.Deductions;
        entity.FinalAmount = model.FinalAmount;
        entity.CustomerPaid = model.CustomerPaid;
        entity.PaymentMode = model.PaymentMode.Trim();
        entity.CustomerRemaining = model.CustomerRemaining;
        entity.CommissionType = model.CommissionType.Trim();
        entity.CommissionValue = model.CommissionValue;
        entity.CommissionAmount = model.CommissionAmount ?? 0;
        entity.ProviderEarning = model.ProviderEarning ?? 0;
        entity.Status = model.Status.Trim();
        if (isPostAcceptanceCancel)
        {
            entity.CancelReason = model.CancelReason!.Trim();

            // Same staff action, same save — mirrors AssignProviderAsync/RespondToAssignmentAsync
            // (reject branch), which also update both tables atomically from one call site.
            // See docs/status-workflow.md.
            var linkedRequest = await _db.CustomerServiceRequests
                .FirstOrDefaultAsync(r => r.Uid == entity.RequestUid, cancellationToken);
            if (linkedRequest != null)
            {
                linkedRequest.Status = "Cancelled";
                linkedRequest.CancelReason = model.CancelReason!.Trim();
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        await ReplaceMaterialItemsAsync(entity.Uid, model.MaterialItems, cancellationToken);

        if (isPostAcceptanceCancel)
        {
            await PublishProviderCancellationNotificationAsync(entity.Uid, entity.ProviderUid, entity.CancelReason, cancellationToken);
        }

        if (string.Equals(entity.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            await CompleteBookingSideEffectsAsync(entity, cancellationToken);
        }

        return (true, null);
    }

    private async Task ReplaceMaterialItemsAsync(
        int bookingUid,
        List<BookingMaterialItemInputVm> items,
        CancellationToken cancellationToken)
    {
        var existing = await _db.BookingMaterialItems
            .Where(i => i.BookingUid == bookingUid)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.BookingMaterialItems.RemoveRange(existing);
        }

        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.ItemName)))
        {
            _db.BookingMaterialItems.Add(new BookingMaterialItem
            {
                BookingUid = bookingUid,
                ItemName = item.ItemName.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Amount,
                CreatedOn = DateTime.Now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishProviderCancellationNotificationAsync(
        int bookingUid,
        int providerUid,
        string? cancelReason,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();

            var providerName = await db.Providers
                .AsNoTracking()
                .Where(p => p.Uid == providerUid)
                .Select(p => p.FullName)
                .FirstOrDefaultAsync(cancellationToken);

            await notifications.NotifyProviderCancellationAsync(
                bookingUid,
                providerName,
                cancelReason,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish admin cancellation notification for booking {BookingUid}.", bookingUid);
        }
    }

    private async Task CompleteBookingSideEffectsAsync(ServiceBooking entity, CancellationToken cancellationToken)
    {
        var request = await _db.CustomerServiceRequests
            .FirstOrDefaultAsync(r => r.Uid == entity.RequestUid, cancellationToken);
        if (request != null && !string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            request.Status = "Completed";
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _payments.RecordBookingCompletionAsync(entity, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceBookings
            .FirstOrDefaultAsync(b => b.Uid == id, cancellationToken);

        if (entity == null) return (false, "Booking not found.");

        var hasLedger = await _db.PaymentLedgers
            .AnyAsync(l => l.BookingUid == id, cancellationToken);

        if (hasLedger)
        {
            return (false, "Cannot delete this booking because it has payment ledger entries.");
        }

        _db.ServiceBookings.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<BookingFormVm> PopulateFormAsync(
        BookingFormVm model,
        CancellationToken cancellationToken = default)
    {
        model.Requests = await GetRequestOptionsAsync(cancellationToken);
        model.Providers = await GetProviderOptionsAsync(cancellationToken);
        model.StatusOptions = GetStatusOptions();
        model.PaymentModeOptions = ValidPaymentModes
            .Select(m => new SelectListItem
            {
                Value = m,
                Text = m switch
                {
                    "CashToProvider" => "Cash to Provider",
                    "OnlineToCompany" => "Online to Company",
                    _ => m
                }
            })
            .ToList();
        model.CommissionTypeOptions = ValidCommissionTypes
            .Select(t => new SelectListItem { Value = t, Text = t })
            .ToList();

        if (model.RequestUid > 0 &&
            (model.LockRequestFields || string.IsNullOrWhiteSpace(model.ServiceTitle)))
        {
            var requestInfo = await _db.CustomerServiceRequests
                .AsNoTracking()
                .Where(r => r.Uid == model.RequestUid)
                .Select(r => new
                {
                    r.ServiceTitle,
                    ClientName = r.Client.FullName,
                    ServiceType = r.Category.CategoryName,
                    r.ServiceDescription
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (requestInfo != null)
            {
                model.ServiceTitle = requestInfo.ServiceTitle;
                model.ClientName = requestInfo.ClientName;
                model.ServiceType = requestInfo.ServiceType;
                if (string.IsNullOrWhiteSpace(model.ServiceDetail))
                {
                    model.ServiceDetail = requestInfo.ServiceDescription;
                }
            }
        }

        return model;
    }

    public async Task<AssignProviderVm?> GetAssignProviderFormAsync(
        int requestUid,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.CustomerServiceRequests
            .AsNoTracking()
            .Where(r => r.Uid == requestUid)
            .Select(r => new
            {
                r.Uid,
                r.Status,
                r.ServiceTitle,
                r.ServiceDescription,
                r.EstimatedBudget,
                r.CategoryUid,
                ClientName = r.Client.FullName,
                ClientCity = r.Client.City,
                CategoryName = r.Category.CategoryName,
                ServiceAddress = r.ClientAddress.AddressTitle + " - " + r.ClientAddress.FullAddress
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request == null) return null;

        if (!RequestStatusConstants.IsUnassigned(request.Status))
        {
            return null;
        }

        var alreadyBooked = await _db.ServiceBookings
            .AnyAsync(b => b.RequestUid == requestUid && b.Status != "Rejected", cancellationToken);
        if (alreadyBooked) return null;

        var matchingProviders = await _db.Providers
            .AsNoTracking()
            .Where(p => p.User.IsActive && p.IsVerified
                && p.ProviderCategories.Any(pc => pc.CategoryUid == request.CategoryUid))
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.FullName + " (" + p.Category.CategoryName + ")"
                    + (p.City != null && p.City != "" ? " — " + p.City : "")
            })
            .ToListAsync(cancellationToken);

        // Override list: same client city (category ignored). Empty if client has no city.
        var clientCity = (request.ClientCity ?? string.Empty).Trim();
        List<SelectListItem> cityProviders;
        if (string.IsNullOrWhiteSpace(clientCity))
        {
            cityProviders = new List<SelectListItem>();
        }
        else
        {
            var cityLower = clientCity.ToLower();
            cityProviders = await _db.Providers
                .AsNoTracking()
                .Where(p => p.User.IsActive
                    && p.City != null
                    && p.City.ToLower() == cityLower
                    && p.IsVerified)
                .OrderBy(p => p.FullName)
                .Select(p => new SelectListItem
                {
                    Value = p.Uid.ToString(),
                    Text = p.FullName + " (" + p.Category.CategoryName + ")"
                        + (p.City != null && p.City != "" ? " — " + p.City : "")
                })
                .ToListAsync(cancellationToken);
        }

        var estimated = request.EstimatedBudget ?? 0m;
        var resolved = await _commissionRules.ResolveAsync(
            providerUid: null,
            categoryUid: request.CategoryUid,
            cancellationToken: cancellationToken);

        var vm = new AssignProviderVm
        {
            RequestUid = request.Uid,
            CategoryUid = request.CategoryUid,
            ClientName = request.ClientName,
            ClientCity = string.IsNullOrWhiteSpace(clientCity) ? null : clientCity,
            ServiceTitle = request.ServiceTitle,
            CategoryName = request.CategoryName,
            ServiceAddress = request.ServiceAddress,
            Status = request.Status,
            EstimatedBudget = request.EstimatedBudget,
            ServiceDetail = request.ServiceDescription,
            EstimatedAmount = estimated,
            VisitCharges = 0,
            AdditionalCharges = 0,
            Deductions = 0,
            CustomerPaid = 0,
            CommissionType = resolved.CommissionType,
            CommissionValue = resolved.CommissionValue,
            CommissionSourceLabel = resolved.SourceLabel,
            PaymentMode = "CashToProvider",
            HasCategoryMatch = matchingProviders.Count > 0,
            ShowAllProviders = matchingProviders.Count == 0,
            Providers = matchingProviders,
            AllProviders = cityProviders,
            PaymentModeOptions = ValidPaymentModes
                .Select(m => new SelectListItem
                {
                    Value = m,
                    Text = m switch
                    {
                        "CashToProvider" => "Cash to Provider",
                        "OnlineToCompany" => "Online to Company",
                        _ => m
                    }
                })
                .ToList(),
            CommissionTypeOptions = ValidCommissionTypes
                .Select(t => new SelectListItem
                {
                    Value = t,
                    Text = t switch
                    {
                        "Percent" => "Percent",
                        "Fixed" => "Fixed",
                        _ => t
                    }
                })
                .ToList()
        };

        ApplyAssignTotals(vm);
        return vm;
    }

    public async Task<(bool Success, string? Error)> AssignProviderAsync(
        AssignProviderVm model,
        CancellationToken cancellationToken = default)
    {
        var request = await _db.CustomerServiceRequests
            .FirstOrDefaultAsync(r => r.Uid == model.RequestUid, cancellationToken);

        if (request == null)
        {
            return (false, "Service request not found.");
        }

        if (!RequestStatusConstants.IsUnassigned(request.Status))
        {
            return (false, "Only initiated requests can be assigned to a provider.");
        }

        var alreadyBooked = await _db.ServiceBookings
            .AnyAsync(b => b.RequestUid == model.RequestUid && b.Status != "Rejected", cancellationToken);
        if (alreadyBooked)
        {
            return (false, "This request already has a booking.");
        }

        var providerUids = (model.ProviderUids ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (providerUids.Count == 0)
        {
            return (false, "Select at least one provider.");
        }

        var providers = await _db.Providers
            .AsNoTracking()
            .Where(p => providerUids.Contains(p.Uid)
                && p.IsVerified)
            .Select(p => new
            {
                p.Uid,
                p.FullName,
                p.City,
                HasCategory = p.ProviderCategories.Any(pc => pc.CategoryUid == request.CategoryUid)
            })
            .ToListAsync(cancellationToken);

        if (providers.Count != providerUids.Count)
        {
            return (false, "One or more selected providers do not exist or are not verified.");
        }

        var clientCity = await _db.Clients
            .AsNoTracking()
            .Where(c => c.Uid == request.ClientUid)
            .Select(c => c.City)
            .FirstOrDefaultAsync(cancellationToken);

        if (!model.ShowAllProviders)
        {
            var mismatched = providers
                .Where(p => !p.HasCategory)
                .Select(p => p.FullName)
                .ToList();
            if (mismatched.Count > 0)
            {
                return (false, "Some selected providers do not match this request's service category. Enable \"Show city providers\" to override.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(clientCity))
            {
                return (false, "Client has no City set. Set the client's city before using city-matched providers.");
            }

            var cityMismatched = providers
                .Where(p => !string.Equals(p.City?.Trim(), clientCity.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(p => p.FullName)
                .ToList();
            if (cityMismatched.Count > 0)
            {
                return (false, $"Some selected providers are not in the client's city ({clientCity}).");
            }
        }

        if (!ValidPaymentModes.Contains(model.PaymentMode))
        {
            return (false, "Invalid payment method.");
        }

        if (!ValidCommissionTypes.Contains(model.CommissionType))
        {
            return (false, "Invalid commission type.");
        }

        if (model.CommissionType.Equals("Percent", StringComparison.OrdinalIgnoreCase) && model.CommissionValue > 100)
        {
            return (false, "Percent commission value cannot exceed 100.");
        }

        ApplyAssignTotals(model);

        var totalsError = ValidateBookingTotals(model.FinalAmount, model.CommissionAmount, model.CustomerPaid);
        if (totalsError != null)
        {
            return (false, totalsError);
        }

        var serviceDetail = string.IsNullOrWhiteSpace(model.ServiceDetail)
            ? request.ServiceDescription
            : model.ServiceDetail.Trim();

        // Single SaveChanges covers all booking inserts + request status update atomically
        // (avoids SqlServerRetryingExecutionStrategy transaction restrictions).
        foreach (var providerUid in providerUids)
        {
            _db.ServiceBookings.Add(new ServiceBooking
            {
                RequestUid = request.Uid,
                ClientUid = request.ClientUid,
                ProviderUid = providerUid,
                ServiceDetail = serviceDetail,
                EstimatedAmount = model.EstimatedAmount,
                VisitCharges = model.VisitCharges,
                AdditionalCharges = model.AdditionalCharges,
                Deductions = model.Deductions,
                FinalAmount = model.FinalAmount,
                CustomerPaid = model.CustomerPaid,
                PaymentMode = model.PaymentMode.Trim(),
                CustomerRemaining = model.CustomerRemaining,
                CommissionType = model.CommissionType.Trim(),
                CommissionValue = model.CommissionValue,
                CommissionAmount = model.CommissionAmount,
                ProviderEarning = model.ProviderEarning,
                Status = "Pending",
                CreatedOn = DateTime.Now
            });
        }

        request.Status = "Assigned";
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RespondToAssignmentAsync(
        int bookingUid,
        int providerUid,
        bool accept,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.ServiceBookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Uid == bookingUid, cancellationToken);

        if (booking == null || booking.ProviderUid != providerUid)
        {
            return (false, "Booking not found.");
        }

        if (!accept && string.IsNullOrWhiteSpace(reason))
        {
            return (false, "Reject reason is required when rejecting a booking.");
        }

        if (accept)
        {
            // Idempotent double-tap: this same provider already accepted this booking.
            if (string.Equals(booking.Status, "Accepted", StringComparison.OrdinalIgnoreCase)
                || string.Equals(booking.Status, "In Progress", StringComparison.OrdinalIgnoreCase)
                || string.Equals(booking.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(booking.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return (true, null);
            }

            if (!string.Equals(booking.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                // Cancelled/Rejected/Superseded by the time this provider responded.
                return (false, "This job has already been assigned to another provider.");
            }

            var passcode = Random.Shared.Next(1000, 10000).ToString();
            var acceptedOn = DateTime.Now;

            // Atomic conditional claim: only succeeds if still Pending at the DB level,
            // so two concurrent accepts on sibling bookings can't both win (ExecuteUpdateAsync
            // is a single atomic statement, compatible with the SqlServerRetryingExecutionStrategy
            // constraint that rules out explicit BeginTransactionAsync elsewhere in this class).
            var claimed = await _db.ServiceBookings
                .Where(b => b.Uid == bookingUid && b.Status == "Pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, "Accepted")
                    .SetProperty(b => b.AcceptedOn, acceptedOn)
                    .SetProperty(b => b.Passcode, passcode), cancellationToken);

            if (claimed == 0)
            {
                // Lost the race between the read above and this update.
                return (false, "This job has already been assigned to another provider.");
            }

            // Supersede sibling Pending bookings for the same request — they're no longer
            // available to the other providers they were fanned out to.
            await _db.ServiceBookings
                .Where(b => b.RequestUid == booking.RequestUid && b.Uid != bookingUid && b.Status == "Pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, "Cancelled")
                    .SetProperty(b => b.CancelReason, "Assigned to another provider"), cancellationToken);

            await _db.CustomerServiceRequests
                .Where(r => r.Uid == booking.RequestUid)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, "Accepted"), cancellationToken);

            return (true, null);
        }
        else
        {
            if (!string.Equals(booking.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "This booking is no longer awaiting a response.");
            }

            var rejected = await _db.ServiceBookings
                .Where(b => b.Uid == bookingUid && b.Status == "Pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, "Rejected")
                    .SetProperty(b => b.RejectReason, reason!.Trim()), cancellationToken);

            if (rejected == 0)
            {
                return (false, "This booking is no longer awaiting a response.");
            }

            // Only reset the request back to Initiated (for staff reassignment) if this was the
            // last remaining Pending sibling — if other providers are still pending, leave the
            // request Assigned so they can still respond.
            var stillPending = await _db.ServiceBookings
                .AnyAsync(b => b.RequestUid == booking.RequestUid && b.Status == "Pending", cancellationToken);

            if (!stillPending)
            {
                await _db.CustomerServiceRequests
                    .Where(r => r.Uid == booking.RequestUid)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RequestStatusConstants.Initiated), cancellationToken);
            }

            await PublishProviderCancellationNotificationAsync(bookingUid, providerUid, reason!.Trim(), cancellationToken);

            return (true, null);
        }
    }

    public async Task<(bool Success, string? Error)> StartJobAsync(
        int bookingUid,
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.ServiceBookings
            .FirstOrDefaultAsync(b => b.Uid == bookingUid, cancellationToken);

        if (booking == null || booking.ProviderUid != providerUid)
        {
            return (false, "Booking not found.");
        }

        if (!string.Equals(booking.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "This booking is not awaiting start.");
        }

        booking.Status = "In Progress";
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> VerifyCompletionPasscodeAsync(
        int bookingUid,
        int providerUid,
        string passcode,
        decimal actualAmountPaid,
        string? paymentMode,
        decimal? labourAmount = null,
        List<Models.Api.VerifyCompletionMaterialItemDto>? materialItems = null,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.ServiceBookings
            .FirstOrDefaultAsync(b => b.Uid == bookingUid, cancellationToken);

        if (booking == null || booking.ProviderUid != providerUid)
        {
            return (false, "Booking not found.");
        }

        if (!string.Equals(booking.Status, "Accepted", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(booking.Status, "In Progress", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "This booking is not awaiting completion.");
        }

        if (string.IsNullOrWhiteSpace(booking.Passcode)
            || !string.Equals(booking.Passcode, passcode?.Trim(), StringComparison.Ordinal))
        {
            return (false, "Incorrect passcode.");
        }

        if (actualAmountPaid < 0)
        {
            return (false, "Amount paid cannot be negative.");
        }

        if (paymentMode != null)
        {
            if (!ValidPaymentModes.Contains(paymentMode))
            {
                return (false, "Invalid payment mode.");
            }
            booking.PaymentMode = paymentMode;
        }

        // The amount actually collected on-site is the real final bill — assignment-time
        // estimates (FinalAmount/CommissionAmount/ProviderEarning) must be recomputed off
        // it, otherwise the commission split desyncs from the cash that actually moved
        // (e.g. price changed on-site, or only a partial amount was collected).
        booking.CustomerPaid = actualAmountPaid;
        booking.FinalAmount = actualAmountPaid;
        booking.CustomerRemaining = ComputeCustomerRemaining(booking.FinalAmount, actualAmountPaid);

        // TODO(remove after old app retired): labourAmount is optional so an old app build
        // (which never sends it) keeps today's exact legacy behavior — commission computed on
        // the WHOLE amount collected. Only once the app sends labourAmount does completion
        // switch to the labour-only commission base. Once every live build always sends it,
        // collapse this to always use labourAmount ?? 0.
        var commissionBase = labourAmount ?? booking.FinalAmount;
        var (commissionAmount, providerEarning) = ResolveCommissionAmounts(
            commissionBase,
            booking.CommissionType,
            booking.CommissionValue,
            null,
            null);
        booking.CommissionAmount = commissionAmount;
        booking.ProviderEarning = providerEarning;

        if (labourAmount.HasValue)
        {
            booking.LabourAmount = labourAmount.Value;
        }

        booking.Status = "Completed";
        booking.CompletedOn = DateTime.Now;

        await _db.SaveChangesAsync(cancellationToken);

        if (materialItems is { Count: > 0 })
        {
            await ReplaceMaterialItemsAsync(
                booking.Uid,
                materialItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
                    .Select(i => new BookingMaterialItemInputVm
                    {
                        ItemName = i.ItemName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    })
                    .ToList(),
                cancellationToken);
        }

        await CompleteBookingSideEffectsAsync(booking, cancellationToken);

        return (true, null);
    }

    private async Task<string?> ValidateAsync(BookingFormVm model, CancellationToken cancellationToken)
    {
        var requestExists = await _db.CustomerServiceRequests
            .AnyAsync(r => r.Uid == model.RequestUid, cancellationToken);
        if (!requestExists) return "Selected service request does not exist.";

        var providerExists = await _db.Providers
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (!providerExists) return "Selected provider does not exist.";

        if (!ValidStatuses.Contains(model.Status)) return "Invalid status value.";
        if (!ValidPaymentModes.Contains(model.PaymentMode)) return "Invalid payment mode.";
        if (!ValidCommissionTypes.Contains(model.CommissionType)) return "Invalid commission type.";

        if (model.CommissionType.Equals("Percent", StringComparison.OrdinalIgnoreCase) && model.CommissionValue > 100)
        {
            return "Percent commission value cannot exceed 100.";
        }

        return null;
    }

    private static void ApplyAssignTotals(AssignProviderVm model)
    {
        model.FinalAmount = ComputeFinalBill(
            model.EstimatedAmount,
            model.VisitCharges,
            model.AdditionalCharges,
            model.Deductions);
        model.CustomerRemaining = ComputeCustomerRemaining(model.FinalAmount, model.CustomerPaid);

        // LabourAmount is not captured at assignment time (set later at completion/edit), so the
        // commission base here is 0 until an admin fills it in via the booking-edit form.
        var (commissionAmount, providerEarning) = ResolveCommissionAmounts(
            0,
            model.CommissionType,
            model.CommissionValue,
            null,
            null);

        model.CommissionAmount = commissionAmount;
        model.ProviderEarning = providerEarning;
    }

    private static void ApplyBookingTotals(BookingFormVm model)
    {
        // Backward compatible: if only FinalAmount was filled, treat it as EstimatedAmount.
        if (model.EstimatedAmount == 0 && model.VisitCharges == 0 && model.AdditionalCharges == 0
            && model.Deductions == 0 && model.FinalAmount > 0)
        {
            model.EstimatedAmount = model.FinalAmount;
        }

        var materialAmount = model.MaterialItems.Sum(i => i.Amount);

        model.FinalAmount = ComputeFinalBill(
            model.EstimatedAmount,
            model.VisitCharges,
            model.AdditionalCharges,
            model.Deductions) + materialAmount + (model.LabourAmount ?? 0);
        model.CustomerRemaining = ComputeCustomerRemaining(model.FinalAmount, model.CustomerPaid);

        // Commission/ledger are computed off LabourAmount only — material cost passes through
        // to the customer bill (FinalAmount above) but is not commissionable company revenue.
        // LabourAmount unset (null) is treated as a 0 base rather than falling back to the old
        // whole-amount behavior, so it's visibly $0 commission until an admin fills it in.
        var (commissionAmount, providerEarning) = ResolveCommissionAmounts(
            model.LabourAmount ?? 0,
            model.CommissionType,
            model.CommissionValue,
            model.CommissionAmount,
            model.ProviderEarning);

        model.CommissionAmount = commissionAmount;
        model.ProviderEarning = providerEarning;
    }

    private static string? ValidateBookingTotals(decimal finalAmount, decimal? commissionAmount, decimal customerPaid)
    {
        if (finalAmount < 0)
        {
            return "Final bill cannot be negative. Reduce deductions.";
        }

        if (commissionAmount.HasValue && commissionAmount.Value > finalAmount)
        {
            return "Company commission cannot exceed final bill.";
        }

        if (customerPaid < 0)
        {
            return "Customer paid cannot be negative.";
        }

        return null;
    }

    private static decimal ComputeFinalBill(
        decimal estimatedAmount,
        decimal visitCharges,
        decimal additionalCharges,
        decimal deductions) =>
        Math.Round(estimatedAmount + visitCharges + additionalCharges - deductions, 2);

    private static decimal ComputeCustomerRemaining(decimal finalBill, decimal customerPaid) =>
        Math.Round(finalBill - customerPaid, 2);

    private static (decimal CommissionAmount, decimal ProviderEarning) ResolveCommissionAmounts(
        decimal finalAmount,
        string commissionType,
        decimal commissionValue,
        decimal? commissionAmountOverride,
        decimal? providerEarningOverride)
    {
        decimal commissionAmount;
        if (commissionAmountOverride.HasValue && commissionAmountOverride.Value > 0)
        {
            commissionAmount = commissionAmountOverride.Value;
        }
        else if (commissionType.Equals("Percent", StringComparison.OrdinalIgnoreCase))
        {
            commissionAmount = Math.Round(finalAmount * commissionValue / 100m, 2);
        }
        else
        {
            commissionAmount = commissionValue;
        }

        var providerEarning = providerEarningOverride.HasValue && providerEarningOverride.Value > 0
            ? providerEarningOverride.Value
            : Math.Round(finalAmount - commissionAmount, 2);

        return (commissionAmount, providerEarning);
    }
}
