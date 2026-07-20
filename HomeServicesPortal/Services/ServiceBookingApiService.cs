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
        var query = _db.ServiceBookings.AsNoTracking();

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
            .Where(b => b.Uid == bookingUid);

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
        var alreadyBooked = await _db.ServiceBookings
            .AnyAsync(b => b.RequestUid == request.RequestUid, cancellationToken);

        if (alreadyBooked)
        {
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
            return (false, error, null);
        }

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
            Status = request.Status
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

    private static System.Linq.Expressions.Expression<Func<ServiceBooking, ServiceBookingApiDto>> MapToDtoExpression() =>
        b => new ServiceBookingApiDto
        {
            Uid = b.Uid,
            RequestUid = b.RequestUid,
            RequestTitle = b.Request.ServiceTitle,
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
            CreatedOn = b.CreatedOn
        };
}
