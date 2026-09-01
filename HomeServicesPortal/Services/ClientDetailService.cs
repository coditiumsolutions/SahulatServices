using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class ClientDetailService : IClientDetailService
{
    private readonly AppDbContext _db;

    public ClientDetailService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string? Error, ClientDetailApiDto? Data)> GetClientDetailAsync(
        int clientUid,
        CancellationToken cancellationToken = default)
    {
        var client = await _db.Clients
            .AsNoTracking()
            .Where(c => c.Uid == clientUid)
            .Select(c => new ClientDetailApiDto
            {
                Uid = c.Uid,
                UserUid = c.UserUid,
                MobileNo = c.User.MobileNo,
                FullName = c.FullName,
                Cnic = c.Cnic,
                Gender = c.Gender
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (client == null)
        {
            return (false, "Client not found.", null);
        }

        return (true, null, client);
    }

    public async Task<(bool Success, string? Error, ClientDetailApiDto? Data)> UpdateClientDetailAsync(
        UpdateClientDetailRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Uid == request.ClientUid, cancellationToken);

        if (client == null)
        {
            return (false, "Client not found.", null);
        }

        client.FullName = request.FullName.Trim();
        client.Cnic = request.Cnic?.Trim();
        client.Gender = request.Gender?.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return await GetClientDetailAsync(client.Uid, cancellationToken);
    }
}
