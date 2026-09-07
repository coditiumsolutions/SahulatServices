using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface IDashboardService
{
    Task<DashboardVm> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<RequestGraphVm> GetRequestGraphAsync(CancellationToken cancellationToken = default);
}

