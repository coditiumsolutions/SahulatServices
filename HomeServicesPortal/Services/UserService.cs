using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class UserService : IUserService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<UserListVm> GetUsersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        var users = await query
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);

        var result = new UserListVm { Search = search };

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Users.Add(new UserListItemVm
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                LockoutEnabled = user.LockoutEnabled
            });
        }

        return result;
    }

    public async Task<UserDetailsVm?> GetUserDetailsAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDetailsVm
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            Roles = roles.ToList()
        };
    }

    public async Task<UserEditVm?> GetUserForEditAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserEditVm
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            AvailableRoles = await GetAllRolesAsync(cancellationToken)
        };
    }

    public async Task<UserDeleteVm?> GetUserForDeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return new UserDeleteVm
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            Roles = roles.ToList()
        };
    }

    public async Task<List<string>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => r.Name!)
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateUserAsync(
        UserCreateVm model,
        CancellationToken cancellationToken = default)
    {
        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            return (false, new[] { $"Role '{model.Role}' does not exist." });
        }

        var existing = await _userManager.FindByNameAsync(model.UserName);
        if (existing != null)
        {
            return (false, new[] { "Username is already taken." });
        }

        var existingEmail = await _userManager.FindByEmailAsync(model.Email);
        if (existingEmail != null)
        {
            return (false, new[] { "Email is already registered." });
        }

        var user = new IdentityUser
        {
            UserName = model.UserName,
            Email = model.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return (false, roleResult.Errors.Select(e => e.Description));
        }

        _logger.LogInformation("User {UserName} created with role {Role}.", model.UserName, model.Role);
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(
        UserEditVm model,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return (false, new[] { "User not found." });
        }

        if (!await _roleManager.RoleExistsAsync(model.Role))
        {
            return (false, new[] { $"Role '{model.Role}' does not exist." });
        }

        var duplicateName = await _userManager.FindByNameAsync(model.UserName);
        if (duplicateName != null && duplicateName.Id != user.Id)
        {
            return (false, new[] { "Username is already taken." });
        }

        var duplicateEmail = await _userManager.FindByEmailAsync(model.Email);
        if (duplicateEmail != null && duplicateEmail.Id != user.Id)
        {
            return (false, new[] { "Email is already registered." });
        }

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.EmailConfirmed = model.EmailConfirmed;
        user.NormalizedUserName = _userManager.NormalizeName(model.UserName);
        user.NormalizedEmail = _userManager.NormalizeEmail(model.Email);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, updateResult.Errors.Select(e => e.Description));
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                return (false, passwordResult.Errors.Select(e => e.Description));
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.Role))
        {
            if (currentRoles.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, model.Role);
        }

        _logger.LogInformation("User {UserId} updated.", model.Id);
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> DeleteUserAsync(
        string id,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (id == currentUserId)
        {
            return (false, new[] { "You cannot delete your own account." });
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return (false, new[] { "User not found." });
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        _logger.LogInformation("User {UserId} deleted.", id);
        return (true, Array.Empty<string>());
    }
}
