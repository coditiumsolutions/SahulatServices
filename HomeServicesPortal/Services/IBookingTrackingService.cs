using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IBookingTrackingService
{
    Task<List<SelectListItem>> GetBookingOptionsAsync(CancellationToken cancellationToken = default);
    List<SelectListItem> GetStatusOptions();
    Task<BookingTrackingListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<BookingTrackingDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<BookingTrackingFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<BookingTrackingDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(BookingTrackingFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(BookingTrackingFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<BookingTrackingFormVm> PopulateFormAsync(BookingTrackingFormVm model, CancellationToken cancellationToken = default);
}
