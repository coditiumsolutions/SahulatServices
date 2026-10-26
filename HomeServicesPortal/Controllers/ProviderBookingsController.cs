using System.Globalization;
using HomeServicesPortal.Data;
using HomeServicesPortal.Filters;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>Provider "Bookings" tab and booking detail: start, cancel and complete a job.</summary>
[Authorize(Roles = UserTypeConstants.Provider)]
[TypeFilter(typeof(RequireVerifiedProviderFilter))]
public class ProviderBookingsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IServiceBookingApiService _bookings;
    private readonly ILogger<ProviderBookingsController> _logger;

    public ProviderBookingsController(
        AppDbContext db,
        IServiceBookingApiService bookings,
        ILogger<ProviderBookingsController> logger)
    {
        _db = db;
        _bookings = bookings;
        _logger = logger;
    }

    [HttpGet("/provider/bookings")]
    public async Task<IActionResult> Index(string? tab, CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var all = await _bookings.GetBookingsAsync(providerUid.Value, cancellationToken);
        var mine = all.Where(b => !string.Equals(b.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).ToList();

        static bool IsActive(string s) => s is "Pending" or "Accepted" or "In Progress";
        static bool IsCompleted(string s) => s is "Completed" or "Closed";

        var selected = tab?.ToLowerInvariant() switch
        {
            ProviderBookingsVm.TabCompleted => ProviderBookingsVm.TabCompleted,
            ProviderBookingsVm.TabCancelled => ProviderBookingsVm.TabCancelled,
            _ => ProviderBookingsVm.TabActive
        };

        var vm = new ProviderBookingsVm
        {
            Tab = selected,
            ActiveCount = mine.Count(b => IsActive(b.Status)),
            CompletedCount = mine.Count(b => IsCompleted(b.Status)),
            CancelledCount = mine.Count(b => b.Status == "Cancelled"),
            Items = mine
                .Where(b => selected switch
                {
                    ProviderBookingsVm.TabCompleted => IsCompleted(b.Status),
                    ProviderBookingsVm.TabCancelled => b.Status == "Cancelled",
                    _ => IsActive(b.Status)
                })
                .OrderByDescending(b => b.CreatedOn)
                .ToList()
        };

        ViewData["Title"] = "My Bookings";
        return View("ProviderBookings", vm);
    }

    [HttpGet("/provider/bookings/{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var booking = await GetOwnedAsync(id, cancellationToken);
        if (booking == null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Booking #" + booking.Uid;
        return View("ProviderBookingDetail", new ProviderBookingDetailVm { Booking = booking });
    }

    [HttpPost("/provider/bookings/{id:int}/start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id, string? returnTo, CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var (success, error, _) = await _bookings.StartJobAsync(id, providerUid.Value, cancellationToken);
        if (success)
        {
            TempData["ProviderSuccess"] = "Job started";
        }
        else
        {
            TempData["ProviderError"] = error ?? "Failed to start job";
        }

        return Redirect(returnTo == "detail" ? $"/provider/bookings/{id}" : "/provider/bookings");
    }

    [HttpPost("/provider/bookings/{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason, string? otherReason, CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var booking = await GetOwnedAsync(id, cancellationToken);
        if (booking == null)
        {
            return NotFound();
        }

        var back = $"/provider/bookings/{id}";
        if (booking.Status is not ("Accepted" or "In Progress"))
        {
            TempData["ProviderError"] = "This booking can no longer be cancelled.";
            return Redirect(back);
        }

        var chosen = BookingDisplayHelper.ProviderReasons.FirstOrDefault(r => r.Equals(reason?.Trim(), StringComparison.Ordinal));
        if (chosen == null)
        {
            TempData["ProviderError"] = "Please choose a reason.";
            return Redirect(back);
        }

        if (chosen == "Other")
        {
            var more = otherReason?.Trim();
            if (string.IsNullOrEmpty(more))
            {
                TempData["ProviderError"] = "Please tell us more.";
                return Redirect(back);
            }

            chosen = more.Length > 500 ? more[..500] : more;
        }

        // Keep every other field exactly as it is; only the status and reason change.
        var update = new UpdateServiceBookingDto
        {
            BookingUid = id,
            ProviderUid = providerUid.Value,
            ServiceDetail = booking.ServiceDetail,
            EstimatedAmount = booking.EstimatedAmount,
            VisitCharges = booking.VisitCharges,
            AdditionalCharges = booking.AdditionalCharges,
            Deductions = booking.Deductions,
            CustomerPaid = booking.CustomerPaid,
            PaymentMode = booking.PaymentMode,
            CommissionType = booking.CommissionType,
            CommissionValue = booking.CommissionValue,
            Status = "Cancelled",
            CancelReason = chosen
        };

        var (success, error, _) = await _bookings.UpdateBookingAsync(update, cancellationToken);
        if (!success)
        {
            TempData["ProviderError"] = error ?? "Failed to cancel booking";
            return Redirect(back);
        }

        TempData["ProviderSuccess"] = "Booking marked as Cancelled";
        return Redirect("/provider/bookings");
    }

    /// <summary>Verify the customer's passcode and close the job. Not safe to retry, so the page disables the button on submit.</summary>
    [HttpPost("/provider/bookings/{id:int}/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, CompleteBookingForm form, CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        var booking = await GetOwnedAsync(id, cancellationToken);
        if (booking == null)
        {
            return NotFound();
        }

        IActionResult Fail(string message)
        {
            ViewData["Title"] = "Booking #" + booking.Uid;
            return View("ProviderBookingDetail", new ProviderBookingDetailVm { Booking = booking, Form = form, CompleteError = message });
        }

        var passcode = form.Passcode?.Trim() ?? string.Empty;
        var amountOk = TryDecimal(form.Amount, out var amount);
        if (passcode.Length == 0 || !amountOk || amount <= 0)
        {
            return Fail("Enter a valid passcode and an amount greater than 0 to close this job.");
        }

        decimal? labour = null;
        if (!string.IsNullOrWhiteSpace(form.Labour))
        {
            if (!TryDecimal(form.Labour, out var labourValue) || labourValue < 0)
            {
                return Fail("Enter a valid labour charge, or leave it blank.");
            }

            labour = labourValue;
        }

        var paymentMode = form.PaymentMode?.Trim();
        if (string.IsNullOrEmpty(paymentMode))
        {
            paymentMode = null;
        }
        else if (paymentMode is not ("CashToProvider" or "OnlineToCompany"))
        {
            return Fail("Choose a valid payment mode.");
        }

        var items = new List<VerifyCompletionMaterialItemDto>();
        var rows = new[] { form.ItemName.Count, form.ItemQty.Count, form.ItemPrice.Count }.Max();
        for (var i = 0; i < rows; i++)
        {
            var name = (i < form.ItemName.Count ? form.ItemName[i] : null)?.Trim() ?? string.Empty;
            var qtyText = (i < form.ItemQty.Count ? form.ItemQty[i] : null)?.Trim() ?? string.Empty;
            var priceText = (i < form.ItemPrice.Count ? form.ItemPrice[i] : null)?.Trim() ?? string.Empty;
            if (name.Length == 0 && priceText.Length == 0 && (qtyText.Length == 0 || qtyText == "1"))
            {
                continue; // an untouched row
            }

            if (name.Length == 0
                || !int.TryParse(qtyText, NumberStyles.None, CultureInfo.InvariantCulture, out var qty) || qty <= 0
                || !TryDecimal(priceText, out var price) || price < 0)
            {
                return Fail("Enter a valid name, quantity, and price for every material item, or remove the row.");
            }

            items.Add(new VerifyCompletionMaterialItemDto { ItemName = name, Quantity = qty, UnitPrice = price });
        }

        var (success, error, _) = await _bookings.VerifyCompletionAsync(
            id,
            providerUid.Value,
            passcode,
            amount,
            paymentMode,
            labour,
            items.Count > 0 ? items : null,
            cancellationToken);

        if (!success)
        {
            return Fail(error ?? "Failed to verify completion");
        }

        TempData["ProviderSuccess"] = "Booking marked as completed";
        return Redirect("/provider/bookings?tab=completed");
    }

    private async Task<ServiceBookingApiDto?> GetOwnedAsync(int id, CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return null;
        }

        var (ok, _, booking) = await _bookings.GetBookingByIdAsync(id, providerUid.Value, cancellationToken);
        return ok ? booking : null;
    }

    private static bool TryDecimal(string? text, out decimal value) =>
        decimal.TryParse(text?.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
}
