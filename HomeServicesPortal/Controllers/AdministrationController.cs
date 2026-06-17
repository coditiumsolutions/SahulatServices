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

    [HttpGet("/Administration/Users")]
    public async Task<IActionResult> Users(string? search, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUsersAsync(search, cancellationToken);
        return View("Users/Index", vm);
    }

    [HttpGet("/Administration/Users/Create")]
    public async Task<IActionResult> CreateUser(CancellationToken cancellationToken)
    {
        var vm = new UserCreateVm
        {
            AvailableRoles = await _userService.GetAllRolesAsync(cancellationToken)
        };
        return View("Users/Create", vm);
    }

    [HttpPost("/Administration/Users/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserCreateVm model, CancellationToken cancellationToken)
    {
        model.AvailableRoles = await _userService.GetAllRolesAsync(cancellationToken);

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

        TempData["SuccessMessage"] = $"User '{model.UserName}' created successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("/Administration/Users/Details/{id}")]
    public async Task<IActionResult> UserDetails(string id, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUserDetailsAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View("Users/Details", vm);
    }

    [HttpGet("/Administration/Users/Edit/{id}")]
    public async Task<IActionResult> EditUser(string id, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUserForEditAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View("Users/Edit", vm);
    }

    [HttpPost("/Administration/Users/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string id, UserEditVm model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();

        model.AvailableRoles = await _userService.GetAllRolesAsync(cancellationToken);

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

        TempData["SuccessMessage"] = $"User '{model.UserName}' updated successfully.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("/Administration/Users/Delete/{id}")]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        var vm = await _userService.GetUserForDeleteAsync(id, cancellationToken);
        if (vm == null) return NotFound();
        return View("Users/Delete", vm);
    }

    [HttpPost("/Administration/Users/Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(string id, CancellationToken cancellationToken)
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
