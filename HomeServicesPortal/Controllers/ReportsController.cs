using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// No reporting implementation exists yet. All routes show a WIP page instead of 404ing.
/// </summary>
[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ReportsController : Controller
{
    private IActionResult Wip(string title) => View("WorkInProgress", new WorkInProgressVm
    {
        PageTitle = title,
        Reason = "Reporting has not been built yet."
    });

    [HttpGet("/Admin/Reports/Provider")]
    public IActionResult Provider() => Wip("Provider Report");

    [HttpGet("/Admin/Reports/Customer")]
    public IActionResult Customer() => Wip("Customer Report");

    [HttpGet("/Admin/Reports/Bookings")]
    public IActionResult Bookings() => Wip("Booking Report");

    [HttpGet("/Admin/Reports/Payments")]
    public IActionResult Payments() => Wip("Payment Report");

    [HttpGet("/Admin/Reports/ServiceRequests")]
    public IActionResult ServiceRequests() => Wip("Service Request Report");
}
