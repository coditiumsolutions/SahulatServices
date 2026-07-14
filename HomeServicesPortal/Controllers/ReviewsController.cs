using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class ReviewsController : Controller
{
    private readonly IReviewService _service;

    public ReviewsController(IReviewService service)
    {
        _service = service;
    }

    [HttpGet("/Admin/Reviews")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        return View(await _service.GetListAsync(search, sort, sortDir, page, cancellationToken));
    }

    [HttpGet("/Admin/Reviews/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await _service.PopulateFormAsync(new ReviewFormVm(), cancellationToken));
    }

    [HttpPost("/Admin/Reviews/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewFormVm model, CancellationToken cancellationToken)
    {
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create review.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Review created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Reviews/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/Reviews/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Reviews/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReviewFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update review.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Review updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Reviews/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Reviews/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete review.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = "Review deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
