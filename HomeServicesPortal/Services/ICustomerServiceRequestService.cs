using HomeServicesPortal.Models.Api;

namespace HomeServicesPortal.Services;

public interface ICustomerServiceRequestService
{
    Task<IReadOnlyList<CustomerServiceRequestApiDto>> GetRequestsAsync(
        int? clientUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, CustomerServiceRequestApiDto? Data)> GetRequestByIdAsync(
        int requestUid,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, CustomerServiceRequestApiDto? Data)> CreateRequestAsync(
        CreateCustomerServiceRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error, CustomerServiceRequestApiDto? Data)> UpdateRequestAsync(
        UpdateCustomerServiceRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> DeleteRequestAsync(
        int requestUid,
        CancellationToken cancellationToken = default);
}
