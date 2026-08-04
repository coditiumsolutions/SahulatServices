using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// This admin CRUD list is backed by the legacy ProviderAvailability table (SahulatAppDbContext),
/// which is [REMOVED] per db.txt. It is distinct from the live Providers.IsAvailable/AvailableTiming
/// flag used by the mobile-facing availability API. All routes show a WIP page until migrated.
/// </summary>
[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ProviderAvailabilityController : Controller
{
    private IActionResult Wip() => View("WorkInProgress", new WorkInProgressVm
    {
        PageTitle = "P-Availability",
        Reason = "Provider Availability still reads from a legacy database table that no longer exists in the live schema."
    });

    [HttpGet("/Admin/ProviderAvailability")]
    public IActionResult Index() => Wip();

    [HttpGet("/Admin/ProviderAvailability/Create")]
    public IActionResult Create() => Wip();

    [HttpGet("/Admin/ProviderAvailability/Details/{id:int}")]
    public IActionResult Details(int id) => Wip();

    [HttpGet("/Admin/ProviderAvailability/Edit/{id:int}")]
    public IActionResult Edit(int id) => Wip();

    [HttpGet("/Admin/ProviderAvailability/Delete/{id:int}")]
    public IActionResult Delete(int id) => Wip();
}
