using System.Security.Claims;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Controllers;

/// <summary>Client portal profile and ClientAddresses CRUD.</summary>
[Authorize(Roles = UserTypeConstants.Client)]
public class ClientProfileController : Controller
{
    private readonly AppDbContext _db;
    private readonly IClientAddressService _addresses;
    private readonly ILogger<ClientProfileController> _logger;

    public ClientProfileController(
        AppDbContext db,
        IClientAddressService addresses,
        ILogger<ClientProfileController> logger)
    {
        _db = db;
        _addresses = addresses;
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

        return new ClientProfileVm
        {
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
