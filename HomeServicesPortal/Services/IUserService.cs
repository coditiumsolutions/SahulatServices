using HomeServicesPortal.Models.ViewModels;

namespace HomeServicesPortal.Services;

public interface IUserService
{
    Task<UserListVm> GetUsersAsync(string? search, CancellationToken cancellationToken = default);
    Task<UserDetailsVm?> GetUserDetailsAsync(string id, CancellationToken cancellationToken = default);
    Task<UserEditVm?> GetUserForEditAsync(string id, CancellationToken cancellationToken = default);
    Task<UserDeleteVm?> GetUserForDeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<List<string>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, IEnumerable<string> Errors)> CreateUserAsync(UserCreateVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(UserEditVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, IEnumerable<string> Errors)> DeleteUserAsync(string id, string? currentUserId, CancellationToken cancellationToken = default);
}
