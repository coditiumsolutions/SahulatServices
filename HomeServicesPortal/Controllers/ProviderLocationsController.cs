using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ProviderLocationsController : Controller
{
    private readonly IProviderLocationService _service;

    public ProviderLocationsController(IProviderLocationService service)
    {
        _service = service;
    }

    [HttpGet("/ProviderLocations")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/ProviderLocations/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new ProviderLocationFormVm
        {
            Providers = await _service.GetProviderOptionsAsync(cancellationToken)
        });
    }

    [HttpPost("/ProviderLocations/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderLocationFormVm model, CancellationToken cancellationToken)
    {
        model.Providers = await _service.GetProviderOptionsAsync(cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create location.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Provider location created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderLocations/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/ProviderLocations/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderLocations/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProviderLocationFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        model.Providers = await _service.GetProviderOptionsAsync(cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update location.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Provider location updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderLocations/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderLocations/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete location.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = "Provider location deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
