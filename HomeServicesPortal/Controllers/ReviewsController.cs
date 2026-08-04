using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// Backed by the legacy Reviews, Booking, and Customer tables (SahulatAppDbContext),
/// all [REMOVED] per db.txt. All routes show a WIP page until migrated to the live schema.
/// </summary>
[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ReviewsController : Controller
{
    private IActionResult Wip() => View("WorkInProgress", new WorkInProgressVm
    {
        PageTitle = "Reviews",
        Reason = "Reviews still reads from legacy database tables that no longer exist in the live schema."
    });

    [HttpGet("/Admin/Reviews")]
    public IActionResult Index() => Wip();

    [HttpGet("/Admin/Reviews/Create")]
    public IActionResult Create() => Wip();

    [HttpGet("/Admin/Reviews/Details/{id:int}")]
    public IActionResult Details(int id) => Wip();

    [HttpGet("/Admin/Reviews/Edit/{id:int}")]
    public IActionResult Edit(int id) => Wip();

    [HttpGet("/Admin/Reviews/Delete/{id:int}")]
    public IActionResult Delete(int id) => Wip();
}
