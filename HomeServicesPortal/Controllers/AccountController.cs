using System.Security.Claims;
using HomeServicesPortal.Data;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// Admin portal login using UsersLogin + Staff (UserType = Staff).
/// </summary>
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext db, ILogger<AccountController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Displays the login page. Redirects authenticated users to the admin portal.
    /// </summary>
    [HttpGet("/adminportal")]
    [HttpGet("/Account/Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Validates Staff credentials from UsersLogin (UserType must be Staff) and Staff profile.
    /// </summary>
    [HttpPost("/adminportal")]
    [HttpPost("/Account/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var loginName = model.Username.Trim();

        var user = await _db.UsersLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.MobileNo == loginName, cancellationToken);

        if (user == null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            _logger.LogWarning("Staff login failed: invalid credentials for {LoginName}.", loginName);
            ModelState.AddModelError(string.Empty, "Invalid login name or password.");
            return View(model);
        }

        if (!user.UserType.Equals(UserTypeConstants.Staff, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Staff login denied: UserType={UserType} for {LoginName}.", user.UserType, loginName);
            ModelState.AddModelError(string.Empty, "Only Staff accounts can sign in to the admin portal.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Account is inactive. Contact support.");
            return View(model);
        }

        var staff = await _db.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserUid == user.Uid, cancellationToken);

        if (staff == null)
        {
            _logger.LogWarning("Staff login failed: no Staff profile for user UID {UserUid}.", user.Uid);
            ModelState.AddModelError(string.Empty, "Staff profile not found.");
            return View(model);
        }

        var trackedUser = await _db.UsersLogins.FirstAsync(u => u.Uid == user.Uid, cancellationToken);
        trackedUser.LastLogin = DateTime.Now;
        await _db.SaveChangesAsync(cancellationToken);

        var role = staff.IsAdmin ? "Super Admin" : "Admin";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Uid.ToString()),
            new(ClaimTypes.Name, staff.FullName),
            new(ClaimTypes.MobilePhone, user.MobileNo),
            new("UserType", UserTypeConstants.Staff),
            new("StaffUid", staff.Uid.ToString()),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Role, "Dispatcher"),
            new(ClaimTypes.Role, "Customer Support")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        _logger.LogInformation("Staff {FullName} ({LoginName}) signed in to admin portal.", staff.FullName, loginName);
        return RedirectToLocal(model.ReturnUrl);
    }

    /// <summary>
    /// Signs the staff user out and redirects to the login page.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Staff logged out.");
        return Redirect("/adminportal");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("/Admin");
    }
}
