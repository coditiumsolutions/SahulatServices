using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ServiceBookingApiService : IServiceBookingApiService
{
    private readonly AppDbContext _db;
    private readonly IBookingService _bookingService;

    public ServiceBookingApiService(AppDbContext db, IBookingService bookingService)
    {
        _db = db;
        _bookingService = bookingService;
    }

    public async Task<IReadOnlyList<ServiceBookingApiDto>> GetBookingsAsync(
        int? providerUid,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ServiceBookings.AsNoTracking().Where(b => b.Status != "Rejected");

        if (providerUid.HasValue)
        {
            query = query.Where(b => b.ProviderUid == providerUid.Value);
        }

        return await query
            .OrderByDescending(b => b.CreatedOn)
            .ThenByDescending(b => b.Uid)
            .Select(MapToDtoExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> GetBookingByIdAsync(
        int bookingUid,
        int? providerUid,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ServiceBookings
            .AsNoTracking()
            .Where(b => b.Uid == bookingUid && b.Status != "Rejected");

        if (providerUid.HasValue)
        {
            query = query.Where(b => b.ProviderUid == providerUid.Value);
        }

        var booking = await query
            .Select(MapToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        if (booking == null)
        {
            return (false, "Booking not found.", null);
        }

        return (true, null, booking);
    }

    public async Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> CreateBookingAsync(
        CreateServiceBookingDto request,
        CancellationToken cancellationToken = default)
    {
        // SERIALIZABLE so a second concurrent CreateBookingAsync call for the same RequestUid
        // (retried API call, double-tap) blocks on the AnyAsync read inside the transaction until
        // the first commits/rolls back, instead of both calls seeing "no booking yet" and both
        // creating one. _bookingService shares this same scoped AppDbContext, so its CreateAsync
        // write is enlisted in the same transaction. Wrapped in CreateExecutionStrategy().ExecuteAsync
        // (matches AuthService.ExecuteInTransactionAsync / PaymentService.RecordBookingCompletionAsync)
        // since EnableRetryOnFailure is on in Development and a bare BeginTransactionAsync throws
        // under SqlServerRetryingExecutionStrategy — confirmed by testing this locally.
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<(bool Success, string? Error, ServiceBookingApiDto? Data)>(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

            var alreadyBooked = await _db.ServiceBookings
                .AnyAsync(b => b.RequestUid == request.RequestUid, cancellationToken);

            if (alreadyBooked)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, "This service request already has a booking.", null);
            }

            var form = new BookingFormVm
            {
                RequestUid = request.RequestUid,
                ProviderUid = request.ProviderUid,
                ServiceDetail = request.ServiceDetail,
                EstimatedAmount = request.EstimatedAmount,
                VisitCharges = request.VisitCharges,
                AdditionalCharges = request.AdditionalCharges,
                Deductions = request.Deductions,
                CustomerPaid = request.CustomerPaid,
                PaymentMode = request.PaymentMode,
                CommissionType = request.CommissionType,
                CommissionValue = request.CommissionValue,
                Status = request.Status
            };

            var (success, error) = await _bookingService.CreateAsync(form, cancellationToken);
            if (!success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, error, null);
            }

            await transaction.CommitAsync(cancellationToken);

            var booking = await _db.ServiceBookings
                .AsNoTracking()
                .Where(b => b.RequestUid == request.RequestUid)
                .Select(MapToDtoExpression())
                .FirstOrDefaultAsync(cancellationToken);

            if (booking == null)
            {
                return (false, "Booking was created but could not be loaded.", null);
            }

            return (true, null, booking);
        });
    }

    public async Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> UpdateBookingAsync(
        UpdateServiceBookingDto request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.ServiceBookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Uid == request.BookingUid, cancellationToken);

        if (existing == null)
        {
            return (false, "Booking not found.", null);
        }

        var form = new BookingFormVm
        {
            Uid = request.BookingUid,
            RequestUid = existing.RequestUid,
            ProviderUid = request.ProviderUid,
            ServiceDetail = request.ServiceDetail,
            EstimatedAmount = request.EstimatedAmount,
            VisitCharges = request.VisitCharges,
            AdditionalCharges = request.AdditionalCharges,
            Deductions = request.Deductions,
            CustomerPaid = request.CustomerPaid,
            PaymentMode = request.PaymentMode,
            CommissionType = request.CommissionType,
            CommissionValue = request.CommissionValue,
            Status = request.Status,
            CancelReason = request.CancelReason
        };

        var (success, error) = await _bookingService.UpdateAsync(form, cancellationToken);
        if (!success)
        {
            return (false, error, null);
        }

        return await GetBookingByIdAsync(request.BookingUid, null, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> DeleteBookingAsync(
        int bookingUid,
        int? providerUid,
        CancellationToken cancellationToken = default)
    {
        if (providerUid.HasValue)
        {
            var belongsToProvider = await _db.ServiceBookings
                .AnyAsync(b => b.Uid == bookingUid && b.ProviderUid == providerUid.Value, cancellationToken);

            if (!belongsToProvider)
            {
                return (false, "Booking not found.");
            }
        }

        return await _bookingService.DeleteAsync(bookingUid, cancellationToken);
    }

    public async Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> RespondToBookingAsync(
        int bookingUid,
        int providerUid,
        bool accept,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var (success, error) = await _bookingService.RespondToAssignmentAsync(bookingUid, providerUid, accept, reason, cancellationToken);
        if (!success)
        {
            return (false, error, null);
        }

        // Rejected bookings are filtered out of the normal client/provider-facing lookups,
        // but the provider who just rejected this one still needs their own result back.
        var booking = await _db.ServiceBookings
            .AsNoTracking()
            .Where(b => b.Uid == bookingUid && b.ProviderUid == providerUid)
            .Select(MapToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        if (booking == null)
        {
            return (false, "Booking was updated but could not be loaded.", null);
        }

        return (true, null, booking);
    }

    public async Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> StartJobAsync(
        int bookingUid,
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        var (success, error) = await _bookingService.StartJobAsync(bookingUid, providerUid, cancellationToken);
        if (!success)
        {
            return (false, error, null);
        }

        return await GetBookingByIdAsync(bookingUid, providerUid, cancellationToken);
    }

    public async Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> VerifyCompletionAsync(
        int bookingUid,
        int providerUid,
        string passcode,
        decimal actualAmountPaid,
        string? paymentMode,
        decimal? labourAmount = null,
        List<VerifyCompletionMaterialItemDto>? materialItems = null,
        CancellationToken cancellationToken = default)
    {
        var (success, error) = await _bookingService.VerifyCompletionPasscodeAsync(
            bookingUid, providerUid, passcode, actualAmountPaid, paymentMode, labourAmount, materialItems, cancellationToken);
        if (!success)
        {
            return (false, error, null);
        }

        return await GetBookingByIdAsync(bookingUid, providerUid, cancellationToken);
    }

    public async Task<(bool Success, string? Error, List<BookingMaterialItemApiDto>? Data)> GetMaterialItemsAsync(
        int bookingUid,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.ServiceBookings.AsNoTracking().AnyAsync(b => b.Uid == bookingUid, cancellationToken);
        if (!exists)
        {
            return (false, "Booking not found.", null);
        }

        var items = await _db.BookingMaterialItems
            .AsNoTracking()
            .Where(i => i.BookingUid == bookingUid)
            .OrderBy(i => i.Uid)
            .Select(i => new BookingMaterialItemApiDto
            {
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Amount = i.Amount
            })
            .ToListAsync(cancellationToken);

        return (true, null, items);
    }

    public async Task<(bool Success, string? Error, List<BookingMaterialItemApiDto>? Data)> UpdateMaterialItemsAsync(
        int bookingUid,
        List<VerifyCompletionMaterialItemDto> materialItems,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.ServiceBookings.FirstOrDefaultAsync(b => b.Uid == bookingUid, cancellationToken);
        if (booking == null)
        {
            return (false, "Booking not found.", null);
        }

        var existing = await _db.BookingMaterialItems
            .Where(i => i.BookingUid == bookingUid)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _db.BookingMaterialItems.RemoveRange(existing);
        }

        foreach (var item in (materialItems ?? new List<VerifyCompletionMaterialItemDto>())
                 .Where(i => !string.IsNullOrWhiteSpace(i.ItemName)))
        {
            var quantity = item.Quantity <= 0 ? 1 : item.Quantity;
            _db.BookingMaterialItems.Add(new BookingMaterialItem
            {
                BookingUid = bookingUid,
                ItemName = item.ItemName.Trim(),
                Quantity = quantity,
                UnitPrice = item.UnitPrice,
                Amount = Math.Round(quantity * item.UnitPrice, 2),
                CreatedOn = DateTime.Now
            });
        }

        // Recompute FinalAmount/CustomerRemaining from the new material total — commission is
        // unaffected (it's LabourAmount-based, not tied to materials).
        var materialTotal = await _db.BookingMaterialItems
            .Where(i => i.BookingUid == bookingUid)
            .SumAsync(i => (decimal?)i.Amount, cancellationToken) ?? 0m;

        // Recompute using the currently-saved base charges (Estimated/Visit/Additional/Deductions
        // + Labour), matching BookingService's ComputeFinalBill formula.
        booking.FinalAmount = Math.Round(
            booking.EstimatedAmount + (booking.LabourAmount ?? 0) + materialTotal
                + booking.VisitCharges + booking.AdditionalCharges - booking.Deductions, 2);
        booking.CustomerRemaining = Math.Round(booking.FinalAmount - booking.CustomerPaid, 2);

        await _db.SaveChangesAsync(cancellationToken);

        return await GetMaterialItemsAsync(bookingUid, cancellationToken);
    }

    private static readonly string[] ContactVisibleStatuses = ["Accepted", "In Progress", "Completed", "Closed"];

    private System.Linq.Expressions.Expression<Func<ServiceBooking, ServiceBookingApiDto>> MapToDtoExpression() =>
        b => new ServiceBookingApiDto
        {
            Uid = b.Uid,
            RequestUid = b.RequestUid,
            RequestTitle = b.Request.ServiceTitle,
            PreferredServiceDate = b.Request.PreferredServiceDate,
            PreferredServiceTime = b.Request.PreferredServiceTime,
            ClientUid = b.ClientUid,
            ClientName = b.Client.FullName,
            ProviderUid = b.ProviderUid,
            ProviderName = b.Provider.FullName,
            ServiceDetail = b.ServiceDetail,
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
            RejectReason = b.RejectReason,
            CancelReason = b.CancelReason,
            AcceptedOn = b.AcceptedOn,
            CompletedOn = b.CompletedOn,
            CreatedOn = b.CreatedOn,
            ProviderMobileNo = ContactVisibleStatuses.Contains(b.Status) ? b.Provider.User.MobileNo : null,
            ProviderProfilePhotoPath = ContactVisibleStatuses.Contains(b.Status)
                ? _db.ProviderDocuments.Where(d => d.ProviderUid == b.ProviderUid).Select(d => d.ProfilePhotoPath).FirstOrDefault()
                : null,
            ProviderCnic = ContactVisibleStatuses.Contains(b.Status) ? b.Provider.Cnic : null,
            ClientMobileNo = ContactVisibleStatuses.Contains(b.Status) ? b.Client.User.MobileNo : null,
            // Address is intentionally NOT gated by ContactVisibleStatuses: a provider needs the
            // client's address to decide whether to accept a still-Pending booking at all.
            ClientAddressTitle = b.Request.ClientAddress.AddressTitle,
            ClientFullAddress = b.Request.ClientAddress.FullAddress,
            ClientArea = b.Request.ClientAddress.Area,
            ClientCity = b.Request.ClientAddress.City
        };
}
