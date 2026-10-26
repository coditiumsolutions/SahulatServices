using System.Security.Claims;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>
/// Public website login/signup for Client and Provider accounts (UsersLogin).
/// Staff/admin must use /adminportal — this controller never accepts Staff.
/// </summary>
public class PublicAccountController : Controller
{
    private static readonly string[] Genders = { "Male", "Female", "Other" };

    private readonly AppDbContext _db;
    private readonly IConfigurationEntryService _configurations;
    private readonly ILogger<PublicAccountController> _logger;

    public PublicAccountController(
        AppDbContext db,
        IConfigurationEntryService configurations,
        ILogger<PublicAccountController> logger)
    {
        _db = db;
        _configurations = configurations;
        _logger = logger;
    }

    [HttpGet("/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (IsPublicUserSignedIn())
        {
            return RedirectToDashboardForCurrentUser(returnUrl);
        }

        // Admin/Staff must use /adminportal — never treat this page as admin login.
        if (IsStaffSignedIn())
        {
            return Redirect("/Admin");
        }

        return View(new PublicLoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(PublicLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (mobileOk, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(model.MobileNo);
        if (!mobileOk)
        {
            ModelState.AddModelError(nameof(model.MobileNo), mobileError ?? "Invalid mobile number.");
            return View(model);
        }

        var user = await _db.UsersLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.MobileNo == mobileNo, cancellationToken);

        if (user == null || !PasswordHasher.Verify(model.Password, user.PasswordHash))
        {
            _logger.LogWarning("Public login failed: invalid credentials for {MobileNo}.", mobileNo);
            ModelState.AddModelError(string.Empty, "Invalid mobile number or password.");
            return View(model);
        }

        // Admin panel is separate and sensitive — Staff/Admin cannot authenticate on this public login.
        if (user.UserType.Equals(UserTypeConstants.Staff, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                string.Empty,
                "Admin and staff accounts cannot sign in here. Use the admin portal instead.");
            return View(model);
        }

        if (!user.UserType.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase)
            && !user.UserType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Only Client and Provider accounts can sign in here.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Account is inactive. Contact support.");
            return View(model);
        }

        var (fullName, profileId) = await ResolvePublicProfileAsync(user, cancellationToken);
        if (profileId == 0 || string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError(string.Empty, "User profile not found. Please contact support.");
            return View(model);
        }

        var trackedUser = await _db.UsersLogins.FirstAsync(u => u.Uid == user.Uid, cancellationToken);
        trackedUser.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await SignInPublicUserAsync(user, fullName, profileId, model.RememberMe);

        _logger.LogInformation(
            "Public user {FullName} ({MobileNo}, {UserType}) signed in.",
            fullName, mobileNo, user.UserType);

        return RedirectToDashboard(user.UserType, model.ReturnUrl);
    }

    [HttpGet("/signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup(CancellationToken cancellationToken)
    {
        if (IsPublicUserSignedIn())
        {
            return RedirectToDashboardForCurrentUser();
        }

        if (IsStaffSignedIn())
        {
            return Redirect("/Admin");
        }

        return View(await BuildSignupVmAsync(new PublicSignupViewModel(), cancellationToken));
    }

    [HttpPost("/signup")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(PublicSignupViewModel model, CancellationToken cancellationToken)
    {
        model = await BuildSignupVmAsync(model, cancellationToken);

        var userType = model.UserType?.Trim() ?? string.Empty;
        var isClient = userType.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase);
        var isProvider = userType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase);

        if (!isClient && !isProvider)
        {
            ModelState.AddModelError(nameof(model.UserType), "Please select Client or Provider.");
        }

        var gender = Genders.FirstOrDefault(g => g.Equals(model.Gender?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (gender == null && ModelState.GetFieldValidationState(nameof(model.Gender)) != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
        {
            ModelState.AddModelError(nameof(model.Gender), "Please select a gender.");
        }

        if (!model.AcceptTerms)
        {
            ModelState.AddModelError(nameof(model.AcceptTerms), "You must accept the Terms & Conditions to create an account.");
        }

        var categoryIds = model.CategoryUids.Where(id => id > 0).Distinct().ToList();
        if (isProvider)
        {
            if (string.IsNullOrWhiteSpace(model.Cnic))
            {
                ModelState.AddModelError(nameof(model.Cnic), "Required");
            }

            if (categoryIds.Count == 0)
            {
                ModelState.AddModelError(nameof(model.CategoryUids), "Please select at least one category");
            }
            else if (categoryIds.Count > PublicSignupViewModel.MaxCategories)
            {
                ModelState.AddModelError(nameof(model.CategoryUids), $"You can select up to {PublicSignupViewModel.MaxCategories} categories.");
            }
            else if (categoryIds.Count == 1)
            {
                model.PrimaryCategoryUid = categoryIds[0];
            }
            else if (!model.PrimaryCategoryUid.HasValue || !categoryIds.Contains(model.PrimaryCategoryUid.Value))
            {
                ModelState.AddModelError(nameof(model.PrimaryCategoryUid), "Choose your primary category.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(nameof(model.Description), "Required");
            }
        }

        var (mobileOk, mobileNo, mobileError) = MobileNumberHelper.ValidateAndNormalize(model.MobileNo);
        if (!mobileOk)
        {
            ModelState.AddModelError(nameof(model.MobileNo), mobileError ?? "Invalid mobile number.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _db.UsersLogins
            .AsNoTracking()
            .AnyAsync(u => u.MobileNo == mobileNo, cancellationToken);
        if (existing)
        {
            ModelState.AddModelError(nameof(model.MobileNo), "This mobile number is already registered. Please log in.");
            return View(model);
        }

        if (isProvider)
        {
            var cnic = model.Cnic!.Trim();
            var cnicTaken = await _db.Providers.AsNoTracking()
                .AnyAsync(p => p.Cnic == cnic, cancellationToken);
            if (cnicTaken)
            {
                ModelState.AddModelError(nameof(model.Cnic), "This CNIC is already registered to another provider.");
                return View(model);
            }

            var validCategories = await _db.ServiceCategories.AsNoTracking()
                .CountAsync(c => categoryIds.Contains(c.Uid) && c.IsActive, cancellationToken);
            if (validCategories != categoryIds.Count)
            {
                ModelState.AddModelError(nameof(model.CategoryUids), "One or more selected categories are invalid or inactive.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.City))
            {
                var cityOptions = await _configurations.GetValuesByKeyAsync("Cities", cancellationToken);
                if (!cityOptions.Any(c => c.Equals(model.City.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError(nameof(model.City), "Select a city from the list.");
                    return View(model);
                }
            }
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        UsersLogin? createdUser = null;
        var createdProviderUid = 0;
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                var user = new UsersLogin
                {
                    MobileNo = mobileNo,
                    PasswordHash = PasswordHasher.Hash(model.Password),
                    UserType = isProvider ? UserTypeConstants.Provider : UserTypeConstants.Client,
                    IsActive = true,
                    // Web signup has no OTP step; mark verified so the account can sign in here and in the app.
                    IsVerified = true,
                    CreatedOn = DateTime.UtcNow
                };

                _db.UsersLogins.Add(user);
                await _db.SaveChangesAsync(cancellationToken);
                createdUser = user;

                var fullName = model.FullName.Trim();
                if (isClient)
                {
                    _db.Clients.Add(new Client
                    {
                        UserUid = user.Uid,
                        FullName = fullName,
                        Gender = gender,
                        CreatedOn = DateTime.UtcNow
                    });
                }
                else
                {
                    var provider = new Provider
                    {
                        UserUid = user.Uid,
                        MobileNo = mobileNo,
                        FullName = fullName,
                        Gender = gender,
                        Cnic = model.Cnic!.Trim(),
                        CategoryUid = model.PrimaryCategoryUid!.Value,
                        City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                        ExperienceYears = model.ExperienceYears ?? 0,
                        Description = model.Description!.Trim(),
                        IsVerified = false,
                        IsAvailable = true,
                        CreatedOn = DateTime.UtcNow
                    };
                    _db.Providers.Add(provider);
                    await _db.SaveChangesAsync(cancellationToken);
                    createdProviderUid = provider.Uid;

                    foreach (var categoryId in categoryIds)
                    {
                        _db.ProviderCategories.Add(new ProviderCategory
                        {
                            ProviderUid = provider.Uid,
                            CategoryUid = categoryId,
                            PrimaryCategory = categoryId == model.PrimaryCategoryUid ? 1 : 0,
                            CreatedOn = DateTime.UtcNow
                        });
                    }
                }

                await _db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Public signup failed for {MobileNo}.", mobileNo);
            ModelState.AddModelError(string.Empty, "Could not create your account. Please try again.");
            return View(model);
        }

        _logger.LogInformation("Public signup created {UserType} account for {MobileNo}.", userType, mobileNo);

        // Like the app, a new provider goes straight on to uploading their documents.
        if (isProvider && createdUser != null && createdProviderUid > 0)
        {
            await SignInPublicUserAsync(createdUser, model.FullName.Trim(), createdProviderUid, rememberMe: false);
            TempData["ProviderSuccess"] = "Account created. Upload your documents so we can verify you.";
            return Redirect("/provider/documents");
        }

        TempData["SignupSuccess"] = "Account created successfully. Please log in.";
        return Redirect("/login");
    }

    [HttpPost("/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Public user logged out.");
        return Redirect("/");
    }

    private async Task<(string FullName, int ProfileId)> ResolvePublicProfileAsync(
        UsersLogin user,
        CancellationToken cancellationToken)
    {
        if (user.UserType.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase))
        {
            var client = await _db.Clients.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserUid == user.Uid, cancellationToken);
            return client == null ? (string.Empty, 0) : (client.FullName, client.Uid);
        }

        if (user.UserType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
        {
            var provider = await _db.Providers.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserUid == user.Uid, cancellationToken);
            return provider == null ? (string.Empty, 0) : (provider.FullName, provider.Uid);
        }

        return (string.Empty, 0);
    }

    private async Task SignInPublicUserAsync(UsersLogin user, string fullName, int profileId, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Uid.ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.MobilePhone, user.MobileNo),
            new("UserType", user.UserType),
            new("ProfileId", profileId.ToString()),
            new(ClaimTypes.Role, user.UserType)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(rememberMe ? 24 * 30 : 8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    }

    private async Task<PublicSignupViewModel> BuildSignupVmAsync(
        PublicSignupViewModel model,
        CancellationToken cancellationToken)
    {
        var categories = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem
            {
                Value = c.Uid.ToString(),
                Text = c.CategoryName,
                Selected = model.CategoryUids.Contains(c.Uid)
            })
            .ToListAsync(cancellationToken);

        var cities = (await _configurations.GetValuesByKeyAsync("Cities", cancellationToken))
            .Select(c => new SelectListItem
            {
                Value = c,
                Text = c,
                Selected = string.Equals(model.City, c, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
        model.Cities = cities;
        model.Categories = categories;
        return model;
    }

    private bool IsPublicUserSignedIn()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var userType = User.FindFirstValue("UserType");
        return userType != null
            && (userType.Equals(UserTypeConstants.Client, StringComparison.OrdinalIgnoreCase)
                || userType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsStaffSignedIn()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var userType = User.FindFirstValue("UserType");
        return userType != null
            && userType.Equals(UserTypeConstants.Staff, StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult RedirectToDashboardForCurrentUser(string? returnUrl = null)
    {
        var userType = User.FindFirstValue("UserType") ?? string.Empty;
        return RedirectToDashboard(userType, returnUrl);
    }

    private IActionResult RedirectToDashboard(string userType, string? returnUrl = null)
    {
        // Honour a safe returnUrl only when it targets the matching dashboard (or a non-admin local path).
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            && !returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/adminportal", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(returnUrl);
        }

        if (userType.Equals(UserTypeConstants.Provider, StringComparison.OrdinalIgnoreCase))
        {
            return Redirect("/provider/dashboard");
        }

        return Redirect("/client/dashboard");
    }
}
