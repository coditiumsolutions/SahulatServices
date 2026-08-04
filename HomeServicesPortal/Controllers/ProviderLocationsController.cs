using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// Data-layer for provider locations still points at the legacy SahulatAppDbContext
/// (ProviderLocations table is [REMOVED] per db.txt). All routes show a WIP page
/// until this is migrated to AppDbContext.
/// </summary>
[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ProviderLocationsController : Controller
{
    private IActionResult Wip() => View("WorkInProgress", new WorkInProgressVm
    {
        PageTitle = "P-Locations",
        Reason = "Provider Locations still reads from a legacy database table that no longer exists in the live schema."
    });

    [HttpGet("/Admin/ProviderLocations")]
    public IActionResult Index() => Wip();

    [HttpGet("/Admin/ProviderLocations/Create")]
    public IActionResult Create() => Wip();

    [HttpGet("/Admin/ProviderLocations/Details/{id:int}")]
    public IActionResult Details(int id) => Wip();

    [HttpGet("/Admin/ProviderLocations/Edit/{id:int}")]
    public IActionResult Edit(int id) => Wip();

    [HttpGet("/Admin/ProviderLocations/Delete/{id:int}")]
    public IActionResult Delete(int id) => Wip();
}
