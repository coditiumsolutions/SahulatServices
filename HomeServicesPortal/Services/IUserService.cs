using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Services;

public interface IUserService
{
    Task<UserListVm> GetUsersAsync(string? search, CancellationToken cancellationToken = default);
    Task<UserDetailsVm?> GetUserDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<UserEditVm?> GetUserForEditAsync(int id, CancellationToken cancellationToken = default);
    Task<UserDeleteVm?> GetUserForDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(List<string> Roles, List<SelectListItem> Categories)> GetFormLookupsAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, IEnumerable<string> Errors)> CreateUserAsync(UserCreateVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(UserEditVm model, CancellationToken cancellationToken = default);
    Task<(bool Success, IEnumerable<string> Errors)> DeleteUserAsync(int id, string? currentUserId, CancellationToken cancellationToken = default);
}
