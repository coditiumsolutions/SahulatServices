using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin,Dispatcher,Customer Support")]
public class BookingsController : Controller
{
    private readonly IBookingService _service;

    public BookingsController(IBookingService service)
    {
        _service = service;
    }

    [HttpGet("/Admin/Bookings")]
    public async Task<IActionResult> Index(string? search, string? sort, string? sortDir, int page = 1, CancellationToken cancellationToken = default)
    {
        var vm = await _service.GetListAsync(search, sort, sortDir, page, cancellationToken);
        return View(vm);
    }

    [HttpGet("/Admin/Bookings/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(await _service.PopulateFormAsync(new BookingFormVm(), cancellationToken));
    }

    [HttpPost("/Admin/Bookings/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingFormVm model, CancellationToken cancellationToken)
    {
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.CreateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to create booking.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Booking created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Bookings/Details/{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet("/Admin/Bookings/Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Bookings/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookingFormVm model, CancellationToken cancellationToken)
    {
        if (id != model.Uid) return BadRequest();
        await _service.PopulateFormAsync(model, cancellationToken);
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _service.UpdateAsync(model, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to update booking.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Booking updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/Admin/Bookings/Delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("/Admin/Bookings/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var vm = await _service.GetForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();

        var (success, error) = await _service.DeleteAsync(id, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to delete booking.");
            return View("Delete", vm);
        }

        TempData["SuccessMessage"] = "Booking deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
