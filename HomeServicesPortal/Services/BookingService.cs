using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class BookingService : IBookingService
{
    private static readonly string[] ValidStatuses = ["Pending", "Accepted", "In Progress", "Completed", "Cancelled"];
    private static readonly string[] ValidPaymentModes = ["CashToProvider", "OnlineToCompany"];
    private static readonly string[] ValidCommissionTypes = ["Percent", "Fixed"];

    private readonly AppDbContext _db;
    private readonly IPaymentService _payments;

    public BookingService(AppDbContext db, IPaymentService payments)
    {
        _db = db;
        _payments = payments;
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
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "date" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var query = _db.ServiceBookings.AsNoTracking();

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
                ? query.OrderByDescending(b => b.CreatedOn)
                : query.OrderBy(b => b.CreatedOn)
        };

        var totalCount = await query.CountAsync(cancellationToken);

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
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BookingDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.ServiceBookings
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
                LedgerCount = _db.PaymentLedgers.Count(l => l.BookingUid == b.Uid)
            })
            .FirstOrDefaultAsync(cancellationToken);
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

        ApplyBookingTotals(model);
        var validationErrorAfterCalc = ValidateBookingTotals(model.FinalAmount, model.CommissionAmount, model.CustomerPaid);
        if (validationErrorAfterCalc != null) return (false, validationErrorAfterCalc);

        // Keep request linkage stable on edit.
        entity.ProviderUid = model.ProviderUid;
        entity.ServiceDetail = string.IsNullOrWhiteSpace(model.ServiceDetail) ? null : model.ServiceDetail.Trim();
        entity.EstimatedAmount = model.EstimatedAmount;
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

        await _db.SaveChangesAsync(cancellationToken);

        if (string.Equals(entity.Status, "Completed", StringComparison.OrdinalIgnoreCase))
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

        return (true, null);
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
                CategoryName = r.Category.CategoryName,
                ServiceAddress = r.ClientAddress.AddressTitle + " - " + r.ClientAddress.FullAddress
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (request == null) return null;

        if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var alreadyBooked = await _db.ServiceBookings
            .AnyAsync(b => b.RequestUid == requestUid, cancellationToken);
        if (alreadyBooked) return null;

        var providers = await _db.Providers
            .AsNoTracking()
            .Where(p => p.User.IsActive && p.CategoryUid == request.CategoryUid)
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem
            {
                Value = p.Uid.ToString(),
                Text = p.FullName + " (" + p.Category.CategoryName + ")"
            })
            .ToListAsync(cancellationToken);

        if (providers.Count == 0)
        {
            providers = await GetProviderOptionsAsync(cancellationToken);
        }

        var estimated = request.EstimatedBudget ?? 0m;
        var vm = new AssignProviderVm
        {
            RequestUid = request.Uid,
            ClientName = request.ClientName,
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
            CommissionType = "Percent",
            CommissionValue = 10,
            PaymentMode = "CashToProvider",
            Providers = providers,
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

        if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Only pending requests can be assigned to a provider.");
        }

        var alreadyBooked = await _db.ServiceBookings
            .AnyAsync(b => b.RequestUid == model.RequestUid, cancellationToken);
        if (alreadyBooked)
        {
            return (false, "This request already has a booking.");
        }

        var providerExists = await _db.Providers
            .AnyAsync(p => p.Uid == model.ProviderUid, cancellationToken);
        if (!providerExists)
        {
            return (false, "Selected provider does not exist.");
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

        // Single SaveChanges covers booking insert + request status update atomically
        // (avoids SqlServerRetryingExecutionStrategy transaction restrictions).
        _db.ServiceBookings.Add(new ServiceBooking
        {
            RequestUid = request.Uid,
            ClientUid = request.ClientUid,
            ProviderUid = model.ProviderUid,
            ServiceDetail = string.IsNullOrWhiteSpace(model.ServiceDetail)
                ? request.ServiceDescription
                : model.ServiceDetail.Trim(),
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
            Status = "Accepted",
            CreatedOn = DateTime.Now
        });

        request.Status = "Assigned";
        await _db.SaveChangesAsync(cancellationToken);
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

        var (commissionAmount, providerEarning) = ResolveCommissionAmounts(
            model.FinalAmount,
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

        model.FinalAmount = ComputeFinalBill(
            model.EstimatedAmount,
            model.VisitCharges,
            model.AdditionalCharges,
            model.Deductions);
        model.CustomerRemaining = ComputeCustomerRemaining(model.FinalAmount, model.CustomerPaid);

        var (commissionAmount, providerEarning) = ResolveCommissionAmounts(
            model.FinalAmount,
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
