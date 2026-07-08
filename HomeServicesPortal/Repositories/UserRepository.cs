using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> MobileExistsAsync(string mobileNo, CancellationToken cancellationToken = default) =>
        _db.UsersLogins.AnyAsync(u => u.MobileNo == mobileNo, cancellationToken);

    public async Task<UsersLogin> CreateUserAsync(UsersLogin user, CancellationToken cancellationToken = default)
    {
        _db.UsersLogins.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<Client> CreateClientAsync(Client client, CancellationToken cancellationToken = default)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(cancellationToken);
        return client;
    }

    public async Task<Provider> CreateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync(cancellationToken);
        return provider;
    }

    public async Task<Staff> CreateStaffAsync(Staff staff, CancellationToken cancellationToken = default)
    {
        _db.Staff.Add(staff);
        await _db.SaveChangesAsync(cancellationToken);
        return staff;
    }

    public Task<UsersLogin?> GetUserByMobileAsync(string mobileNo, CancellationToken cancellationToken = default) =>
        _db.UsersLogins.FirstOrDefaultAsync(u => u.MobileNo == mobileNo, cancellationToken);

    public Task<UsersLogin?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.UsersLogins.FirstOrDefaultAsync(u => u.Uid == userId, cancellationToken);

    public Task<bool> ProviderExistsForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Providers.AnyAsync(p => p.UserUid == userId, cancellationToken);

    public async Task UpdateUserTypeAsync(int userId, string userType, CancellationToken cancellationToken = default)
    {
        var user = await _db.UsersLogins.FirstOrDefaultAsync(u => u.Uid == userId, cancellationToken);
        if (user == null)
        {
            return;
        }

        user.UserType = userType;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Client?> GetClientByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.UserUid == userId, cancellationToken);

    public Task<Provider?> GetProviderByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.UserUid == userId, cancellationToken);

    public Task<Staff?> GetStaffByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.UserUid == userId, cancellationToken);

    public async Task UpdateLastLoginAsync(int userId, DateTime lastLogin, CancellationToken cancellationToken = default)
    {
        var user = await _db.UsersLogins.FirstOrDefaultAsync(u => u.Uid == userId, cancellationToken);
        if (user == null)
        {
            return;
        }

        user.LastLogin = lastLogin;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
