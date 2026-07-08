using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HomeServicesPortal.Services;

public class DashboardService : IDashboardService
{
    private readonly IRepository<ServiceCategory> _categoryRepo;
    private readonly IRepository<ProviderProfile> _providerRepo;
    private readonly IRepository<ProviderAvailability> _availabilityRepo;
    private readonly IRepository<Customer> _customerRepo;
    private readonly IRepository<ServiceRequest> _requestRepo;
    private readonly IRepository<Booking> _bookingRepo;
    private readonly IRepository<Payment> _paymentRepo;
    private readonly IRepository<Review> _reviewRepo;

    public DashboardService(
        IRepository<ServiceCategory> categoryRepo,
        IRepository<ProviderProfile> providerRepo,
        IRepository<ProviderAvailability> availabilityRepo,
        IRepository<Customer> customerRepo,
        IRepository<ServiceRequest> requestRepo,
        IRepository<Booking> bookingRepo,
        IRepository<Payment> paymentRepo,
        IRepository<Review> reviewRepo)
    {
        _categoryRepo = categoryRepo;
        _providerRepo = providerRepo;
        _availabilityRepo = availabilityRepo;
        _customerRepo = customerRepo;
        _requestRepo = requestRepo;
        _bookingRepo = bookingRepo;
        _paymentRepo = paymentRepo;
        _reviewRepo = reviewRepo;
    }

    public async Task<DashboardVm> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        // Run summary queries sequentially — all repositories share one scoped DbContext.
        var totalCategories = await _categoryRepo.Query().CountAsync(cancellationToken);
        var totalProviders = await _providerRepo.Query().CountAsync(cancellationToken);
        var totalCustomers = await _customerRepo.Query().CountAsync(cancellationToken);

        var onlineProviders = await _availabilityRepo.Query()
            .Where(a => a.IsOnline == true)
            .Select(a => a.ProviderUid)
            .Distinct()
            .CountAsync(cancellationToken);

        var todaysRequests = await _requestRepo.Query()
            .Where(r => r.RequestDate != null && r.RequestDate >= today && r.RequestDate < tomorrow)
            .CountAsync(cancellationToken);

        var todaysBookings = await _bookingRepo.Query()
            .Where(b => b.BookingDate != null && b.BookingDate >= today && b.BookingDate < tomorrow)
            .CountAsync(cancellationToken);

        // Schema uses Booking.Status values: Accepted, OnTheWay, Started, Completed, Cancelled.
        // Dashboard requirement uses "Pending bookings". We map Pending -> Accepted.
        var pendingBookings = await _bookingRepo.Query()
            .Where(b => b.Status == "Accepted")
            .CountAsync(cancellationToken);

        var completedBookings = await _bookingRepo.Query()
            .Where(b => b.Status == "Completed")
            .CountAsync(cancellationToken);

        var totalRevenue = await _bookingRepo.Query()
            .Where(b => b.Status == "Completed" && b.FinalAmount != null)
            .SumAsync(b => b.FinalAmount!.Value, cancellationToken);

        var ratedReviewCount = await _reviewRepo.Query()
            .CountAsync(r => r.Rating != null, cancellationToken);

        var averageRating = ratedReviewCount > 0
            ? await _reviewRepo.Query()
                .Where(r => r.Rating != null)
                .AverageAsync(r => (decimal)r.Rating!.Value, cancellationToken)
            : 0m;

        var dashboard = new DashboardVm
        {
            TotalCategories = totalCategories,
            TotalProviders = totalProviders,
            OnlineProviders = onlineProviders,
            TotalCustomers = totalCustomers,
            TodaysRequests = todaysRequests,
            TodaysBookings = todaysBookings,
            PendingBookings = pendingBookings,
            CompletedBookings = completedBookings,
            TotalRevenue = totalRevenue,
            AverageRating = Math.Round(averageRating, 2),
        };

        // Booking status chart
        dashboard.BookingStatusChart = await _bookingRepo.Query()
            .Where(b => b.Status != null)
            .GroupBy(b => b.Status!)
            .Select(g => new ChartPointVm { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync(cancellationToken);

        // Monthly revenue chart (last 6 months)
        var monthsBack = 6;
        var monthStarts = Enumerable.Range(0, monthsBack)
            .Select(i => new DateTime(today.Year, today.Month, 1).AddMonths(-i))
            .OrderBy(d => d)
            .ToList();

        var monthlyRevenue = await _bookingRepo.Query()
            .Where(b => b.Status == "Completed" && b.BookingDate != null && b.FinalAmount != null)
            .Select(b => new
            {
                Month = new DateTime(b.BookingDate!.Value.Year, b.BookingDate.Value.Month, 1),
                Amount = b.FinalAmount!.Value
            })
            .GroupBy(x => x.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        dashboard.MonthlyRevenueChart = monthStarts
            .Select(ms => new MonthlyRevenuePointVm
            {
                MonthLabel = ms.ToString("MMM yyyy"),
                Value = monthlyRevenue
                    .Where(x => x.Month == ms)
                    .Select(x => x.Total)
                    .FirstOrDefault()
            })
            .ToList();

        // Category-wise requests chart (top 5)
        var categoryPerf = await _requestRepo.Query()
            .Where(r => r.CategoryUid != 0)
            .GroupBy(r => r.CategoryUid)
            .Select(g => new { CategoryUid = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var categoryIds = categoryPerf.Select(x => x.CategoryUid).ToList();
        var categories = await _categoryRepo.Query()
            .Where(c => categoryIds.Contains(c.Uid))
            .Select(c => new { c.Uid, c.CategoryName })
            .ToListAsync(cancellationToken);

        dashboard.ServiceCategoryWiseRequestsChart = categoryPerf
            .Select(cp =>
            {
                var category = categories.FirstOrDefault(c => c.Uid == cp.CategoryUid);
                return new CategoryWiseRequestPointVm
                {
                    CategoryName = category?.CategoryName ?? $"Category {cp.CategoryUid}",
                    Value = cp.Count
                };
            })
            .ToList();

        // Provider performance chart (top 5 by completed bookings, with avg rating)
        var providerPerf = await _bookingRepo.Query()
            .Where(b => b.Status == "Completed" && b.ProviderUid != 0)
            .GroupBy(b => b.ProviderUid)
            .Select(g => new { ProviderUid = g.Key, CompletedCount = g.Count() })
            .OrderByDescending(x => x.CompletedCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var providerIds = providerPerf.Select(x => x.ProviderUid).ToList();
        var providers = await _providerRepo.Query()
            .Where(p => providerIds.Contains(p.Uid))
            .Select(p => new { p.Uid, FullName = p.UserU.FullName ?? "—" })
            .ToListAsync(cancellationToken);

        var ratings = await _reviewRepo.Query()
            .Where(r => providerIds.Contains(r.ProviderUid) && r.Rating != null)
            .GroupBy(r => r.ProviderUid)
            .Select(g => new { ProviderUid = g.Key, AvgRating = g.Average(r => (decimal)r.Rating!.Value) })
            .ToListAsync(cancellationToken);

        dashboard.ProviderPerformanceChart = providerPerf
            .Select(pp =>
            {
                var provider = providers.FirstOrDefault(p => p.Uid == pp.ProviderUid);
                var avg = ratings.FirstOrDefault(r => r.ProviderUid == pp.ProviderUid)?.AvgRating ?? 0m;
                return new ProviderPerformancePointVm
                {
                    ProviderName = provider?.FullName ?? $"Provider {pp.ProviderUid}",
                    CompletedBookings = pp.CompletedCount,
                    AverageRating = avg
                };
            })
            .ToList();

        // Latest grids
        dashboard.LatestRequests = await _requestRepo.Query()
            .Where(r => r.RequestDate != null)
            .OrderByDescending(r => r.RequestDate)
            .Take(5)
            .Join(_customerRepo.Query(), r => r.CustomerUid, c => c.Uid, (r, c) => new { r, c })
            .Join(_categoryRepo.Query(), x => x.r.CategoryUid, cat => cat.Uid, (x, cat) => new LatestRequestVm
            {
                Uid = x.r.Uid,
                CustomerName = x.c.FullName,
                CategoryName = cat.CategoryName,
                Status = x.r.Status,
                RequestDate = x.r.RequestDate,
                ServiceAddress = x.r.ServiceAddress
            })
            .ToListAsync(cancellationToken);

        dashboard.LatestBookings = await _bookingRepo.Query()
            .Where(b => b.BookingDate != null)
            .OrderByDescending(b => b.BookingDate)
            .Take(5)
            .Join(_providerRepo.Query(), b => b.ProviderUid, p => p.Uid, (b, p) => new LatestBookingVm
            {
                Uid = b.Uid,
                ProviderName = p.UserU.FullName ?? "—",
                BookingDate = b.BookingDate,
                FinalAmount = b.FinalAmount,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);

        dashboard.LatestPayments = await _paymentRepo.Query()
            .Where(p => p.PaymentDate != null)
            .OrderByDescending(p => p.PaymentDate)
            .Take(5)
            .Join(_bookingRepo.Query(), p => p.BookingUid, b => b.Uid, (p, b) => new LatestPaymentVm
            {
                Uid = p.Uid,
                BookingUid = p.BookingUid,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionNo = p.TransactionNo,
                PaymentDate = p.PaymentDate,
                BookingStatus = b.Status
            })
            .ToListAsync(cancellationToken);

        return dashboard;
    }
}

