using HomeServicesPortal.Data;
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
            .CountAsync(r => r.Status == "Pending" || r.Status == "Assigned" || r.Status == "In Progress", cancellationToken);

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

        dashboard.BookingStatusChart = await _db.CustomerServiceRequests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new ChartPointVm { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        var monthStarts = Enumerable.Range(0, 6)
            .Select(i => new DateTime(today.Year, today.Month, 1).AddMonths(-i))
            .OrderBy(d => d)
            .ToList();

        var rangeStart = monthStarts[0];
        var monthlyBudget = await _db.CustomerServiceRequests
            .AsNoTracking()
            .Where(r => r.CreatedOn >= rangeStart && r.EstimatedBudget != null)
            .Select(r => new { r.CreatedOn, Amount = r.EstimatedBudget!.Value })
            .ToListAsync(cancellationToken);

        dashboard.MonthlyRevenueChart = monthStarts
            .Select(ms =>
            {
                var next = ms.AddMonths(1);
                return new MonthlyRevenuePointVm
                {
                    MonthLabel = ms.ToString("MMM yyyy"),
                    Value = monthlyBudget
                        .Where(x => x.CreatedOn >= ms && x.CreatedOn < next)
                        .Sum(x => x.Amount)
                };
            })
            .ToList();

        dashboard.ServiceCategoryWiseRequestsChart = await _db.CustomerServiceRequests
            .AsNoTracking()
            .GroupBy(r => r.Category.CategoryName)
            .Select(g => new CategoryWiseRequestPointVm
            {
                CategoryName = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToListAsync(cancellationToken);

        dashboard.ProviderPerformanceChart = await _db.Providers
            .AsNoTracking()
            .OrderByDescending(p => p.TotalJobsCompleted)
            .ThenByDescending(p => p.AverageRating)
            .Take(5)
            .Select(p => new ProviderPerformancePointVm
            {
                ProviderName = p.FullName,
                CompletedBookings = p.TotalJobsCompleted,
                AverageRating = p.AverageRating
            })
            .ToListAsync(cancellationToken);

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
