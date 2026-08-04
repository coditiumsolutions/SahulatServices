using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// Backed by the legacy BookingTracking + Booking tables (SahulatAppDbContext), both
/// [REMOVED] per db.txt. All routes show a WIP page until migrated to the live schema.
/// </summary>
[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class BookingTrackingController : Controller
{
    private IActionResult Wip() => View("WorkInProgress", new WorkInProgressVm
    {
        PageTitle = "Booking Tracking",
        Reason = "Booking Tracking still reads from legacy database tables that no longer exist in the live schema."
    });

    [HttpGet("/Admin/BookingTracking")]
    public IActionResult Index() => Wip();

    [HttpGet("/Admin/BookingTracking/Create")]
    public IActionResult Create() => Wip();

    [HttpGet("/Admin/BookingTracking/Details/{id:int}")]
    public IActionResult Details(int id) => Wip();

    [HttpGet("/Admin/BookingTracking/Edit/{id:int}")]
    public IActionResult Edit(int id) => Wip();

    [HttpGet("/Admin/BookingTracking/Delete/{id:int}")]
    public IActionResult Delete(int id) => Wip();
}
