using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface IServiceBookingApiService
{
    Task<IReadOnlyList<ServiceBookingApiDto>> GetBookingsAsync(
        int? providerUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> GetBookingByIdAsync(
        int bookingUid,
        int? providerUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> CreateBookingAsync(
        CreateServiceBookingDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> UpdateBookingAsync(
        UpdateServiceBookingDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteBookingAsync(
        int bookingUid,
        int? providerUid,
        CancellationToken cancellationToken = default);
}
