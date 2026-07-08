using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ClientAddressService : IClientAddressService
{
    private readonly AppDbContext _db;

    public ClientAddressService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ClientAddressApiDto>> GetAddressesAsync(
        int? clientUid,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ClientAddresses.AsNoTracking();

        if (clientUid.HasValue)
        {
            query = query.Where(a => a.ClientUid == clientUid.Value);
        }

        return await query
            .OrderBy(a => a.AddressTitle)
            .Select(MapToDtoExpression())
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error, ClientAddressApiDto? Data)> GetAddressByIdAsync(
        int addressUid,
        CancellationToken cancellationToken = default)
    {
        var address = await _db.ClientAddresses
            .AsNoTracking()
            .Where(a => a.Uid == addressUid)
            .Select(MapToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);

        if (address == null)
        {
            return (false, "Client address not found.", null);
        }

        return (true, null, address);
    }

    public async Task<(bool Success, string? Error, ClientAddressApiDto? Data)> CreateAddressAsync(
        CreateClientAddressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var clientError = await ValidateClientExistsAsync(request.ClientUid, cancellationToken);
        if (clientError != null)
        {
            return (false, clientError, null);
        }

        var address = new ClientAddress
        {
            ClientUid = request.ClientUid,
            AddressTitle = request.AddressTitle.Trim(),
            FullAddress = request.FullAddress.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.ClientAddresses.Add(address);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAddressByIdAsync(address.Uid, cancellationToken);
    }

    public async Task<(bool Success, string? Error, ClientAddressApiDto? Data)> UpdateAddressAsync(
        UpdateClientAddressRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var address = await _db.ClientAddresses
            .FirstOrDefaultAsync(a => a.Uid == request.AddressUid, cancellationToken);

        if (address == null)
        {
            return (false, "Client address not found.", null);
        }

        if (address.ClientUid != request.ClientUid)
        {
            return (false, "ClientUid does not match this address.", null);
        }

        var clientError = await ValidateClientExistsAsync(request.ClientUid, cancellationToken);
        if (clientError != null)
        {
            return (false, clientError, null);
        }

        address.AddressTitle = request.AddressTitle.Trim();
        address.FullAddress = request.FullAddress.Trim();
        address.Area = request.Area.Trim();
        address.City = request.City.Trim();
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAddressByIdAsync(address.Uid, cancellationToken);
    }

    public async Task<(bool Success, string? Error)> DeleteAddressAsync(
        int addressUid,
        CancellationToken cancellationToken = default)
    {
        var address = await _db.ClientAddresses
            .FirstOrDefaultAsync(a => a.Uid == addressUid, cancellationToken);

        if (address == null)
        {
            return (false, "Client address not found.");
        }

        var inUse = await _db.CustomerServiceRequests
            .AsNoTracking()
            .AnyAsync(r => r.ClientAddressUid == addressUid, cancellationToken);

        if (inUse)
        {
            return (false, "Cannot delete address because it is linked to service requests.");
        }

        _db.ClientAddresses.Remove(address);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null);
    }

    private async Task<string?> ValidateClientExistsAsync(int clientUid, CancellationToken cancellationToken)
    {
        var exists = await _db.Clients
            .AsNoTracking()
            .AnyAsync(c => c.Uid == clientUid, cancellationToken);

        return exists ? null : "Client not found.";
    }

    private static System.Linq.Expressions.Expression<Func<ClientAddress, ClientAddressApiDto>> MapToDtoExpression() =>
        a => new ClientAddressApiDto
        {
            Uid = a.Uid,
            ClientUid = a.ClientUid,
            AddressTitle = a.AddressTitle,
            FullAddress = a.FullAddress,
            Area = a.Area,
            City = a.City,
            Latitude = a.Latitude,
            Longitude = a.Longitude
        };
}
