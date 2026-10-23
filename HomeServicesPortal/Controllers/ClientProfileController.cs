using System.Security.Claims;
using HomeServicesPortal.Data;
using HomeServicesPortal.DTOs;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Interfaces;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>Client portal profile and ClientAddresses CRUD.</summary>
[Authorize(Roles = UserTypeConstants.Client)]
public class ClientProfileController : Controller
{
    private readonly AppDbContext _db;
    private static readonly string[] Genders = { "Male", "Female", "Other" };

    private readonly IClientAddressService _addresses;
    private readonly IAuthService _authService;
    private readonly ILogger<ClientProfileController> _logger;

    public ClientProfileController(
        AppDbContext db,
        IClientAddressService addresses,
        IAuthService authService,
        ILogger<ClientProfileController> logger)
    {
        _db = db;
        _addresses = addresses;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("/client/profile")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        var profile = await BuildProfileVmAsync(clientUid.Value, cancellationToken);
        return View("ClientProfile", profile);
    }

    [HttpGet("/client/profile/edit")]
    public async Task<IActionResult> EditProfile(CancellationToken cancellationToken)
    {
        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        var client = await _db.Clients.AsNoTracking().FirstAsync(c => c.Uid == clientUid.Value, cancellationToken);
        return View("ClientProfileEdit", new ClientProfileEditVm
        {
            FullName = client.FullName,
            Gender = client.Gender,
            MobileNo = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty
        });
    }

    [HttpPost("/client/profile/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(ClientProfileEditVm model, CancellationToken cancellationToken)
    {
        model.MobileNo = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty;

        var gender = Genders.FirstOrDefault(g => g.Equals(model.Gender?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (gender == null && ModelState.GetFieldValidationState(nameof(model.Gender)) != Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
        {
            ModelState.AddModelError(nameof(model.Gender), "Please select a gender.");
        }

        if (!ModelState.IsValid)
        {
            return View("ClientProfileEdit", model);
        }

        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        var client = await _db.Clients.FirstAsync(c => c.Uid == clientUid.Value, cancellationToken);
        client.FullName = model.FullName.Trim();
        client.Gender = gender;
        await _db.SaveChangesAsync(cancellationToken);

        await RefreshNameClaimAsync(client.FullName);

        TempData["ProfileSuccess"] = "Profile updated successfully.";
        return Redirect("/client/profile");
    }

    [HttpPost("/client/profile/delete-account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            TempData["ProfileError"] = "Password is required.";
            return Redirect("/client/profile");
        }

        var request = new DeleteAccountRequest
        {
            MobileNo = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty,
            Password = password
        };

        var (success, error, _, _) = await _authService.DeleteAccountAsync(request, cancellationToken);
        if (!success)
        {
            TempData["ProfileError"] = error ?? "Failed to delete account";
            return Redirect("/client/profile");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("Client deleted their account from the website.");
        TempData["SignupSuccess"] = "Account deleted successfully.";
        return Redirect("/login");
    }

    [HttpGet("/client/profile/addresses/create")]
    public IActionResult CreateAddress() => View("ClientAddressForm", new ClientPortalAddressFormVm());

    [HttpPost("/client/profile/addresses/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddress(ClientPortalAddressFormVm model, CancellationToken cancellationToken)
    {
        NormalizeOptionalCoords(model);
        if (!ValidateLatLongPair(model))
        {
            return View("ClientAddressForm", model);
        }

        if (!ModelState.IsValid)
        {
            return View("ClientAddressForm", model);
        }

        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        var (success, error, _) = await _addresses.CreateAddressAsync(new CreateClientAddressRequestDto
        {
            ClientUid = clientUid.Value,
            AddressTitle = model.AddressTitle,
            FullAddress = model.FullAddress,
            Area = model.Area,
            City = model.City,
            Latitude = model.Latitude,
            Longitude = model.Longitude
        }, cancellationToken);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not create address.");
            return View("ClientAddressForm", model);
        }

        TempData["ProfileSuccess"] = "Address added successfully.";
        return Redirect("/client/profile");
    }

    [HttpGet("/client/profile/addresses/{addressId:int}/edit")]
    public async Task<IActionResult> EditAddress(int addressId, CancellationToken cancellationToken)
    {
        var owned = await GetOwnedAddressAsync(addressId, cancellationToken);
        if (owned == null)
        {
            return NotFound();
        }

        return View("ClientAddressForm", new ClientPortalAddressFormVm
        {
            Uid = owned.Uid,
            AddressTitle = owned.AddressTitle,
            FullAddress = owned.FullAddress,
            Area = owned.Area,
            City = owned.City,
            Latitude = NormalizeZeroAsNull(owned.Latitude, owned.Longitude)?.lat,
            Longitude = NormalizeZeroAsNull(owned.Latitude, owned.Longitude)?.lng
        });
    }

    [HttpPost("/client/profile/addresses/{addressId:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(
        int addressId,
        ClientPortalAddressFormVm model,
        CancellationToken cancellationToken)
    {
        model.Uid = addressId;
        NormalizeOptionalCoords(model);
        if (!ValidateLatLongPair(model))
        {
            return View("ClientAddressForm", model);
        }

        if (!ModelState.IsValid)
        {
            return View("ClientAddressForm", model);
        }

        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return Redirect("/login");
        }

        var owned = await GetOwnedAddressAsync(addressId, cancellationToken);
        if (owned == null || owned.ClientUid != clientUid.Value)
        {
            return NotFound();
        }

        var (success, error, _) = await _addresses.UpdateAddressAsync(new UpdateClientAddressRequestDto
        {
            AddressUid = addressId,
            ClientUid = clientUid.Value,
            AddressTitle = model.AddressTitle,
            FullAddress = model.FullAddress,
            Area = model.Area,
            City = model.City,
            Latitude = model.Latitude,
            Longitude = model.Longitude
        }, cancellationToken);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Could not update address.");
            return View("ClientAddressForm", model);
        }

        TempData["ProfileSuccess"] = "Address updated successfully.";
        return Redirect("/client/profile");
    }

    [HttpGet("/client/profile/addresses/{addressId:int}/delete")]
    public async Task<IActionResult> DeleteAddress(int addressId, CancellationToken cancellationToken)
    {
        var owned = await GetOwnedAddressAsync(addressId, cancellationToken);
        if (owned == null)
        {
            return NotFound();
        }

        return View("ClientAddressDelete", new ClientPortalAddressDeleteVm
        {
            Uid = owned.Uid,
            AddressTitle = owned.AddressTitle,
            FullAddress = owned.FullAddress,
            Area = owned.Area,
            City = owned.City
        });
    }

    [HttpPost("/client/profile/addresses/{addressId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddressConfirm(int addressId, CancellationToken cancellationToken)
    {
        var owned = await GetOwnedAddressAsync(addressId, cancellationToken);
        if (owned == null)
        {
            return NotFound();
        }

        var (success, error) = await _addresses.DeleteAddressAsync(addressId, cancellationToken);
        if (!success)
        {
            TempData["ProfileError"] = error ?? "Could not delete address.";
            return Redirect("/client/profile");
        }

        _logger.LogInformation("Client {ClientUid} deleted address {AddressUid}.", owned.ClientUid, addressId);
        TempData["ProfileSuccess"] = "Address deleted successfully.";
        return Redirect("/client/profile");
    }

    /// <summary>Re-issues the cookie with the new display name, keeping its other claims and lifetime.</summary>
    private async Task RefreshNameClaimAsync(string fullName)
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (result.Principal == null)
        {
            return;
        }

        var claims = result.Principal.Claims.Where(c => c.Type != ClaimTypes.Name).ToList();
        claims.Add(new Claim(ClaimTypes.Name, fullName));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, result.Properties);
    }

    private async Task<ClientProfileVm> BuildProfileVmAsync(int clientUid, CancellationToken cancellationToken)
    {
        var addresses = await _db.ClientAddresses
            .AsNoTracking()
            .Where(a => a.ClientUid == clientUid)
            .OrderBy(a => a.AddressTitle)
            .Select(a => new ClientAddressListItemVm
            {
                Uid = a.Uid,
                AddressTitle = a.AddressTitle,
                FullAddress = a.FullAddress,
                Area = a.Area,
                City = a.City,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                InUse = a.ServiceRequests.Any()
            })
            .ToListAsync(cancellationToken);

        DateTime? lastLogin = null;
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            lastLogin = await _db.UsersLogins.AsNoTracking()
                .Where(u => u.Uid == userId)
                .Select(u => u.LastLogin)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var gender = await _db.Clients.AsNoTracking()
            .Where(c => c.Uid == clientUid)
            .Select(c => c.Gender)
            .FirstOrDefaultAsync(cancellationToken);

        return new ClientProfileVm
        {
            Gender = gender,
            FullName = User.Identity?.Name ?? "User",
            MobileNo = User.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty,
            LastLogin = lastLogin,
            Addresses = addresses
        };
    }

    private async Task<ClientAddress?> GetOwnedAddressAsync(int addressId, CancellationToken cancellationToken)
    {
        var clientUid = await ResolveClientUidAsync(cancellationToken);
        if (clientUid == null)
        {
            return null;
        }

        return await _db.ClientAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Uid == addressId && a.ClientUid == clientUid.Value, cancellationToken);
    }

    private async Task<int?> ResolveClientUidAsync(CancellationToken cancellationToken)
    {
        if (int.TryParse(User.FindFirstValue("ProfileId"), out var profileId) && profileId > 0)
        {
            var exists = await _db.Clients.AsNoTracking()
                .AnyAsync(c => c.Uid == profileId, cancellationToken);
            if (exists)
            {
                return profileId;
            }
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        return await _db.Clients.AsNoTracking()
            .Where(c => c.UserUid == userId)
            .Select(c => (int?)c.Uid)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private bool ValidateLatLongPair(ClientPortalAddressFormVm model)
    {
        if (model.Latitude.HasValue != model.Longitude.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Latitude and Longitude must both be provided or both left blank.");
            return false;
        }

        return true;
    }

    private static void NormalizeOptionalCoords(ClientPortalAddressFormVm model)
    {
        // Treat blank / (0,0) as unset for the web form.
        if (model.Latitude == 0 && model.Longitude == 0)
        {
            model.Latitude = null;
            model.Longitude = null;
        }
    }

    private static (decimal lat, decimal lng)? NormalizeZeroAsNull(decimal? lat, decimal? lng)
    {
        if (!lat.HasValue || !lng.HasValue)
        {
            return null;
        }

        if (lat == 0 && lng == 0)
        {
            return null;
        }

        return (lat.Value, lng.Value);
    }
}
