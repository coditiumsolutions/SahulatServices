using HomeServicesPortal.DTOs;
using HomeServicesPortal.Entities;

namespace HomeServicesPortal.Interfaces;

public interface IUserRepository
{
    Task<bool> MobileExistsAsync(string mobileNo, CancellationToken cancellationToken = default);

    Task<UsersLogin> CreateUserAsync(UsersLogin user, CancellationToken cancellationToken = default);

    Task<Client> CreateClientAsync(Client client, CancellationToken cancellationToken = default);

    Task<Provider> CreateProviderAsync(Provider provider, CancellationToken cancellationToken = default);

    Task<Staff> CreateStaffAsync(Staff staff, CancellationToken cancellationToken = default);

    Task<UsersLogin?> GetUserByMobileAsync(string mobileNo, CancellationToken cancellationToken = default);

    Task<UsersLogin?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> ProviderExistsForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> ProviderCnicExistsAsync(string cnic, CancellationToken cancellationToken = default);

    Task UpdateUserTypeAsync(int userId, string userType, CancellationToken cancellationToken = default);

    Task<Client?> GetClientByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<Provider?> GetProviderByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<Staff?> GetStaffByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(int userId, DateTime lastLogin, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
