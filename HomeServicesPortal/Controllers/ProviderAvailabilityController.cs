using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ProviderAvailabilityController : Controller
{
    private readonly IProviderAvailabilityService _service;

    public ProviderAvailabilityController(IProviderAvailabilityService service)
    {
        _service = service;
    }

    [HttpGet("/ProviderAvailability")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/ProviderAvailability/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new ProviderAvailabilityFormVm
        {
            Providers = await _service.GetProviderOptionsAsync(cancellationToken)
        });
    }

    [HttpPost("/ProviderAvailability/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProviderAvailabilityFormVm model, CancellationToken cancellationToken)
    {
        model.Providers = await _service.GetProviderOptionsAsync(cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create availability.");
            return View(model);
        }

        TempData["SuccessMessage"] = "P-Availability record created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderAvailability/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/ProviderAvailability/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderAvailability/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProviderAvailabilityFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        model.Providers = await _service.GetProviderOptionsAsync(cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update availability.");
            return View(model);
        }

        TempData["SuccessMessage"] = "P-Availability record updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/ProviderAvailability/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/ProviderAvailability/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete availability.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = "P-Availability record deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
