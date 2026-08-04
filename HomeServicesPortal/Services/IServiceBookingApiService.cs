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

    Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> RespondToBookingAsync(
        int bookingUid,
        int providerUid,
        bool accept,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, ServiceBookingApiDto? Data)> VerifyCompletionAsync(
        int bookingUid,
        int providerUid,
        string passcode,
        decimal actualAmountPaid,
        string? paymentMode,
        CancellationToken cancellationToken = default);
}
