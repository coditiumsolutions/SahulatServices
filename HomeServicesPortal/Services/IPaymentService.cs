using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IPaymentService
{
    Task<List<SelectListItem>> GetBookingOptionsAsync(CancellationToken cancellationToken = default);
    List<SelectListItem> GetPaymentMethodOptions();
    Task<PaymentListVm> GetListAsync(string? search, string? sort, string? sortDir, int page, CancellationToken cancellationToken = default);
    Task<PaymentDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> CreateAsync(PaymentFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpdateAsync(PaymentFormVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentFormVm> PopulateFormAsync(PaymentFormVm model, CancellationToken cancellationToken = default);
}
