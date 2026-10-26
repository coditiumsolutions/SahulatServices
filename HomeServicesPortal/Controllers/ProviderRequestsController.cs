using HomeServicesPortal.Data;
using HomeServicesPortal.Filters;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>Provider "Requests" tab: job offers staff assigned that are waiting for accept or reject.</summary>
[Authorize(Roles = UserTypeConstants.Provider)]
[TypeFilter(typeof(RequireVerifiedProviderFilter))]
public class ProviderRequestsController : Controller
{
    private const string LostRaceError = "This job has already been assigned to another provider.";

    private static readonly string[] RejectReasons =
    {
        "Outside my service area",
        "Fully booked / no availability",
        "Job scope unclear",
        "Price too low",
        "Other"
    };

    private readonly AppDbContext _db;
    private readonly IServiceBookingApiService _bookings;
    private readonly ILogger<ProviderRequestsController> _logger;

    public ProviderRequestsController(
        AppDbContext db,
        IServiceBookingApiService bookings,
        ILogger<ProviderRequestsController> logger)
    {
        _db = db;
        _bookings = bookings;
        _logger = logger;
    }

    [HttpGet("/provider/requests")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = await BuildAsync(cancellationToken);
        if (vm == null)
        {
            return Redirect("/login");
        }

        ViewData["Title"] = "Incoming Requests";
        ViewData["RejectReasons"] = RejectReasons;
        return View("ProviderRequests", vm);
    }

    /// <summary>The list fragment, fetched every 30 seconds by the page (the web has no push).</summary>
    [HttpGet("/provider/requests/list")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var vm = await BuildAsync(cancellationToken);
        if (vm == null)
        {
            return Unauthorized();
        }

        Response.Headers.CacheControl = "no-store";
        return PartialView("_ProviderRequestsList", vm);
    }

    /// <summary>Accept or reject one offer. The provider is always the signed-in one; a retry is safe (see the booking service).</summary>
    [HttpPost("/provider/requests/{id:int}/respond")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(
        int id,
        bool accept,
        string? reason,
        string? otherReason,
        CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return Redirect("/login");
        }

        string? sentReason = null;
        if (!accept)
        {
            var chosen = RejectReasons.FirstOrDefault(r => r.Equals(reason?.Trim(), StringComparison.Ordinal));
            if (chosen == null)
            {
                TempData["ProviderError"] = "Reject reason is required when rejecting a booking.";
                return Redirect("/provider/requests");
            }

            var more = otherReason?.Trim();
            if (chosen == "Other")
            {
                if (string.IsNullOrEmpty(more))
                {
                    TempData["ProviderError"] = "Please tell us more.";
                    return Redirect("/provider/requests");
                }

                chosen = more.Length > 500 ? more[..500] : more;
            }

            sentReason = chosen;
        }

        var (success, error, _) = await _bookings.RespondToBookingAsync(id, providerUid.Value, accept, sentReason, cancellationToken);
        if (success)
        {
            TempData["ProviderSuccess"] = accept ? "Booking accepted" : "Booking rejected";
        }
        else if (string.Equals(error, LostRaceError, StringComparison.Ordinal))
        {
            TempData["ProviderError"] = "Sorry, this job was just taken by another provider";
        }
        else
        {
            TempData["ProviderError"] = error ?? (accept ? "Failed to accept booking" : "Failed to reject booking");
        }

        return Redirect("/provider/requests");
    }

    private async Task<ProviderRequestsVm?> BuildAsync(CancellationToken cancellationToken)
    {
        var providerUid = await User.ResolveProviderUidAsync(_db, cancellationToken);
        if (providerUid == null)
        {
            return null;
        }

        var all = await _bookings.GetBookingsAsync(providerUid.Value, cancellationToken);
        return new ProviderRequestsVm
        {
            Requests = all
                .Where(b => string.Equals(b.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.CreatedOn)
                .Select(ProviderRequestCardVm.From)
                .ToList()
        };
    }
}
