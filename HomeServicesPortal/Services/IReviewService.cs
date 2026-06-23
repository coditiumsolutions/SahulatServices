using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IReviewService
{
    Task<List<SelectListItem>> GetBookingOptionsAsync(CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetCustomerOptionsAsync(CancellationToken cancellationToken = default);
    Task<List<SelectListItem>> GetProviderOptionsAsync(CancellationToken cancellationToken = default);
    Task<ReviewListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<ReviewDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<ReviewFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<ReviewDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(ReviewFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(ReviewFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ReviewFormVm> PopulateFormAsync(ReviewFormVm model, CancellationToken cancellationToken = default);
}
