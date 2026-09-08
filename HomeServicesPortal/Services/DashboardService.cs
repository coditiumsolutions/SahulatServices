using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

/// <summary>
/// Admin dashboard metrics from live core tables (AppDbContext).
/// Booking/payment tables are removed; request-based proxies fill those cards.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RequestGraphVm> GetRequestGraphAsync(CancellationToken cancellationToken = default)
    {
        var initiated = await _db.CustomerServiceRequests
            .AsNoTracking()
            .CountAsync(r =>
                r.Status == RequestStatusConstants.Initiated
                || r.Status == RequestStatusConstants.LegacyPending,
                cancellationToken);

        var pending = await _db.CustomerServiceRequests
            .AsNoTracking()
            .CountAsync(r => _db.ServiceBookings.Any(b =>
                b.RequestUid == r.Uid && b.Status == "Pending"),
                cancellationToken);

        var completed = await _db.CustomerServiceRequests
            .AsNoTracking()
            .CountAsync(r => r.Status == RequestStatusConstants.Completed, cancellationToken);

        return new RequestGraphVm
        {
            InitiatedCount = initiated,
            PendingCount = pending,
            CompletedCount = completed
        };
    }

    public async Task<DashboardVm> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var totalCategories = await _db.ServiceCategories.CountAsync(cancellationToken);
        var totalProviders = await _db.Providers.CountAsync(cancellationToken);
        var onlineProviders = await _db.Providers.CountAsync(p => p.IsAvailable, cancellationToken);
        var totalCustomers = await _db.Clients.CountAsync(cancellationToken);

        var todaysRequests = await _db.CustomerServiceRequests
            .CountAsync(r => r.CreatedOn >= today && r.CreatedOn < tomorrow, cancellationToken);

        var pendingRequests = await _db.CustomerServiceRequests
            .CountAsync(r => r.Status == "Initiated" || r.Status == "Pending" || r.Status == "Assigned" || r.Status == "In Progress", cancellationToken);

        var completedRequests = await _db.CustomerServiceRequests
            .CountAsync(r => r.Status == "Completed", cancellationToken);

        var totalRevenue = await _db.CustomerServiceRequests
            .Where(r => r.Status == "Completed" && r.EstimatedBudget != null)
            .SumAsync(r => r.EstimatedBudget!.Value, cancellationToken);

        var ratedProviders = await _db.Providers
            .Where(p => p.TotalReviews > 0)
            .Select(p => p.AverageRating)
            .ToListAsync(cancellationToken);

        var averageRating = ratedProviders.Count > 0
            ? Math.Round(ratedProviders.Average(), 2)
            : 0m;

        var dashboard = new DashboardVm
        {
            TotalCategories = totalCategories,
            TotalProviders = totalProviders,
            OnlineProviders = onlineProviders,
            TotalCustomers = totalCustomers,
            TodaysRequests = todaysRequests,
            TodaysBookings = todaysRequests,
            PendingBookings = pendingRequests,
            CompletedBookings = completedRequests,
            TotalRevenue = totalRevenue,
            AverageRating = averageRating
        };

        dashboard.LatestRequests = await _db.CustomerServiceRequests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedOn)
            .Take(5)
            .Select(r => new LatestRequestVm
            {
                Uid = r.Uid,
                CustomerName = r.Client.FullName,
                CategoryName = r.Category.CategoryName,
                Status = r.Status,
                RequestDate = r.CreatedOn,
                ServiceAddress = r.ClientAddress.AddressTitle + " - " + r.ClientAddress.FullAddress
            })
            .ToListAsync(cancellationToken);

        dashboard.LatestBookings = new List<LatestBookingVm>();
        dashboard.LatestPayments = new List<LatestPaymentVm>();

        return dashboard;
    }
}
