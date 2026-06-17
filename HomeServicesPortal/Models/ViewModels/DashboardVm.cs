using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.ViewModels;

public class DashboardVm
{
    public int TotalCategories { get; set; }
    public int TotalProviders { get; set; }
    public int OnlineProviders { get; set; }
    public int TotalCustomers { get; set; }
    public int TodaysRequests { get; set; }
    public int TodaysBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CompletedBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRating { get; set; }

    public List<ChartPointVm> BookingStatusChart { get; set; } = new();
    public List<MonthlyRevenuePointVm> MonthlyRevenueChart { get; set; } = new();
    public List<CategoryWiseRequestPointVm> ServiceCategoryWiseRequestsChart { get; set; } = new();
    public List<ProviderPerformancePointVm> ProviderPerformanceChart { get; set; } = new();

    public List<LatestRequestVm> LatestRequests { get; set; } = new();
    public List<LatestBookingVm> LatestBookings { get; set; } = new();
    public List<LatestPaymentVm> LatestPayments { get; set; } = new();
}

public class ChartPointVm
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class MonthlyRevenuePointVm
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class CategoryWiseRequestPointVm
{
    public string CategoryName { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class ProviderPerformancePointVm
{
    public string ProviderName { get; set; } = string.Empty;
    public int CompletedBookings { get; set; }
    public decimal AverageRating { get; set; }
}

public class LatestRequestVm
{
    public int Uid { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DateTime? RequestDate { get; set; }
    public string? ServiceAddress { get; set; }
}

public class LatestBookingVm
{
    public int Uid { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime? BookingDate { get; set; }
    public decimal? FinalAmount { get; set; }
    public string? Status { get; set; }
}

public class LatestPaymentVm
{
    public int Uid { get; set; }
    public int BookingUid { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionNo { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? BookingStatus { get; set; }
}

