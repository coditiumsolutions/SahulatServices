using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IBookingService
{
    Task<List<SelectListItem>> GetRequestOptionsAsync(CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default);
    List<SelectListItem> GetStatusOptions();
    Task<BookingListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<BookingDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<BookingFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<BookingDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(BookingFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(BookingFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<BookingFormVm> PopulateFormAsync(BookingFormVm model, CancellationToken cancellationToken = default);
    Task<AssignProviderVm?> GetAssignProviderFormAsync(int requestUid, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> AssignProviderAsync(AssignProviderVm model, CancellationToken cancellationToken = default);
}
