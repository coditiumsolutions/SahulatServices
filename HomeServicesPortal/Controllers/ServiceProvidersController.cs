using System.Security.Claims;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ServiceProvidersController : Controller
{
    private readonly IServiceProviderService _service;
    private readonly IProviderDocumentService _documents;

    public ServiceProvidersController(
        IServiceProviderService service,
        IProviderDocumentService documents)
    {
        _service = service;
        _documents = documents;
    }

    [HttpGet("/Admin/ServiceProviders")]
    public async Task<IActionResult> Index(string? search, string? verifyStatus, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, verifyStatus, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/Admin/ServiceProviders/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await _service.PopulateFormAsync(new ServiceProviderFormVm(), cancellationToken));
    }

    [HttpPost("/Admin/ServiceProviders/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceProviderFormVm model, CancellationToken cancellationToken)
    {
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create provider.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Job Provider '{model.FullName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/ServiceProviders/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/ServiceProviders/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, string? tab, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        ViewBag.ActiveTab = tab;
        return View(vm);
    }

    [HttpPost("/Admin/ServiceProviders/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceProviderFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update provider.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Job Provider '{model.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/Admin/ServiceProviders/Edit/{id:int}/Documents")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDocuments(int id, ProviderDocumentFormVm model, CancellationToken cancellationToken)
    {
        var provider = await _service.GetForEditAsync(id, cancellationToken);
        if (provider == null) return NotFound();

        // Always bind to the provider being edited (ignore tampered form values).
        model.ProviderUid = id;
        model.MobileNo = provider.MobileNo;
        model.ProviderName = provider.FullName;

        if (model.Uid > 0)
        {
            var existing = provider.DocumentForm;
            if (existing.Uid != model.Uid || existing.ProviderUid != id)
            {
                TempData["ErrorMessage"] = "Document record does not belong to this provider.";
                return RedirectToAction(nameof(Edit), new { id, tab = "documents" });
            }
        }
        else if (provider.DocumentForm.Uid > 0)
        {
            // Race: a docs row appeared; switch to update that row.
            model.Uid = provider.DocumentForm.Uid;
            model.ExistingProfilePhotoPath = provider.DocumentForm.ExistingProfilePhotoPath;
            model.ExistingCnicFrontPath = provider.DocumentForm.ExistingCnicFrontPath;
            model.ExistingCnicBackPath = provider.DocumentForm.ExistingCnicBackPath;
        }

        int? verifiedBy = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var userId))
        {
            verifiedBy = userId;
        }

        var (success, error) = model.Uid > 0
            ? await _documents.UpdateAsync(model, verifiedBy, cancellationToken)
            : await _documents.CreateAsync(model, cancellationToken);

        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to save documents.";
            var vm = await _service.GetForEditAsync(id, cancellationToken);
            if (vm == null) return NotFound();
            // Preserve uploaded-path previews / flags the user just posted where possible.
            vm.DocumentForm.VerificationRemarks = model.VerificationRemarks;
            ViewBag.ActiveTab = "documents";
            ModelState.AddModelError(string.Empty, error ?? "Failed to save documents.");
            return View("Edit", vm);
        }

        TempData["SuccessMessage"] = model.Uid > 0
            ? "Provider documents updated successfully."
            : "Provider documents created successfully.";
        return RedirectToAction(nameof(Edit), new { id, tab = "documents" });
    }

    [HttpGet("/Admin/ServiceProviders/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/ServiceProviders/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete provider.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = $"Job Provider '{vm.FullName}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
