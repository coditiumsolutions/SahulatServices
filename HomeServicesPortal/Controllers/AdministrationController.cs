using System.Security.Claims;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServicesPortal.Controllers;

[Authorize(Roles = "Super Admin,Admin")]
public class AdministrationController : Controller
{
    private readonly IUserService _userService;

    public AdministrationController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("/Admin/Administration/Roles")]
    public IActionResult Roles() => RedirectToAction(nameof(Users));

    [HttpGet("/Admin/Administration/Users")]
    public async Task<IActionResult> Users(string? search, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUsersAsync(search, cancellationToken);
        return View("Users/Index", vm);
    }

    [HttpGet("/Admin/Administration/Users/Create")]
    public async Task<IActionResult> CreateUser(CancellationToken cancellationToken)
    {
        var lookups = await _userService.GetFormLookupsAsync(cancellationToken);
        var vm = new UserCreateVm
        {
            AvailableRoles = lookups.Roles,
            Categories = lookups.Categories
        };
        return View("Users/Create", vm);
    }

    [HttpPost("/Admin/Administration/Users/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserCreateVm model, CancellationToken cancellationToken)
    {
        var lookups = await _userService.GetFormLookupsAsync(cancellationToken);
        model.AvailableRoles = lookups.Roles;
        model.Categories = lookups.Categories;

        if (!ModelState.IsValid)
        {
            return View("Users/Create", model);
        }

        var (success, errors) = await _userService.CreateUserAsync(model, cancellationToken);
        if (!success)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return View("Users/Create", model);
        }

        TempData["SuccessMessage"] = $"User '{model.MobileNo}' ({model.Role}) created successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("/Admin/Administration/Users/Details/{id:int}")]
    public async Task<IActionResult> UserDetails(int id, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUserDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View("Users/Details", vm);
    }

    [HttpGet("/Admin/Administration/Users/Edit/{id:int}")]
    public async Task<IActionResult> EditUser(int id, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUserForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View("Users/Edit", vm);
    }

    [HttpPost("/Admin/Administration/Users/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, UserEditVm model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();

        var lookups = await _userService.GetFormLookupsAsync(cancellationToken);
        model.AvailableRoles = lookups.Roles;
        model.Categories = lookups.Categories;

        if (!ModelState.IsValid)
        {
            return View("Users/Edit", model);
        }

        var (success, errors) = await _userService.UpdateUserAsync(model, cancellationToken);
        if (!success)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return View("Users/Edit", model);
        }

        TempData["SuccessMessage"] = $"User '{model.MobileNo}' updated successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("/Admin/Administration/Users/Delete/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUserForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View("Users/Delete", vm);
    }

    [HttpPost("/Admin/Administration/Users/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(int id, CancellationToken cancellationToken)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var (success, errors) = await _userService.DeleteUserAsync(id, currentUserId, cancellationToken);

        if (!success)
        {
            var vm = await _userService.GetUserForDeleteAsync(id, cancellationToken);
            if (vm == null) return NotFound();

            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return View("Users/Delete", vm);
        }

        TempData["SuccessMessage"] = "User deleted successfully.";
        return RedirectToAction(nameof(Users));
    }
}
