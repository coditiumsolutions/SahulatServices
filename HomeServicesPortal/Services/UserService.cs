using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserListVm> GetUsersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = _db.UsersLogins
            .AsNoTracking()
            .Include(u => u.Client)
            .Include(u => u.Provider)
            .Include(u => u.Staff)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.MobileNo.Contains(term) ||
                u.UserType.Contains(term) ||
                (u.Client != null && u.Client.FullName.Contains(term)) ||
                (u.Provider != null && u.Provider.FullName.Contains(term)) ||
                (u.Staff != null && u.Staff.FullName.Contains(term)));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedOn)
            .ToListAsync(cancellationToken);

        return new UserListVm
        {
            Search = search,
            Users = users.Select(u => new UserListItemVm
            {
                Id = u.Uid,
                MobileNo = u.MobileNo,
                FullName = ResolveFullName(u),
                Role = PortalRoleConstants.FromUser(u.UserType, u.Staff?.IsAdmin == true),
                IsActive = u.IsActive,
                IsVerified = u.IsVerified,
                CreatedOn = u.CreatedOn,
                LastLogin = u.LastLogin
            }).ToList()
        };
    }

    public async Task<UserDetailsVm?> GetUserDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.UsersLogins
            .AsNoTracking()
            .Include(u => u.Client)
            .Include(u => u.Provider!)
                .ThenInclude(p => p.Category)
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Uid == id, cancellationToken);

        if (user == null) return null;

        return new UserDetailsVm
        {
            Id = user.Uid,
            MobileNo = user.MobileNo,
            FullName = ResolveFullName(user),
            Role = PortalRoleConstants.FromUser(user.UserType, user.Staff?.IsAdmin == true),
            UserType = user.UserType,
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            CreatedOn = user.CreatedOn,
            LastLogin = user.LastLogin,
            Cnic = user.Client?.Cnic ?? user.Provider?.Cnic,
            CategoryName = user.Provider?.Category?.CategoryName
        };
    }

    public async Task<UserEditVm?> GetUserForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.UsersLogins
            .AsNoTracking()
            .Include(u => u.Client)
            .Include(u => u.Provider)
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Uid == id, cancellationToken);

        if (user == null) return null;

        var lookups = await GetFormLookupsAsync(cancellationToken);
        return new UserEditVm
        {
            Id = user.Uid,
            MobileNo = user.MobileNo,
            FullName = ResolveFullName(user) ?? string.Empty,
            Role = PortalRoleConstants.FromUser(user.UserType, user.Staff?.IsAdmin == true),
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            Cnic = user.Client?.Cnic ?? user.Provider?.Cnic,
            CategoryUid = user.Provider?.CategoryUid,
            AvailableRoles = lookups.Roles,
            Categories = lookups.Categories
        };
    }

    public async Task<UserDeleteVm?> GetUserForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.UsersLogins
            .AsNoTracking()
            .Include(u => u.Client)
            .Include(u => u.Provider)
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Uid == id, cancellationToken);

        if (user == null) return null;

        return new UserDeleteVm
        {
            Id = user.Uid,
            MobileNo = user.MobileNo,
            FullName = ResolveFullName(user),
            Role = PortalRoleConstants.FromUser(user.UserType, user.Staff?.IsAdmin == true)
        };
    }

    public async Task<(List<string> Roles, List<SelectListItem> Categories)> GetFormLookupsAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _db.ServiceCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new SelectListItem
            {
                Value = c.Uid.ToString(),
                Text = c.CategoryName
            })
            .ToListAsync(cancellationToken);

        return (PortalRoleConstants.All.ToList(), categories);
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateUserAsync(
        UserCreateVm model,
        CancellationToken cancellationToken = default)
    {
        if (!PortalRoleConstants.IsValid(model.Role))
        {
            return (false, new[] { "Invalid role selected." });
        }

        var mobile = model.MobileNo.Trim();
        if (await _db.UsersLogins.AnyAsync(u => u.MobileNo == mobile, cancellationToken))
        {
            return (false, new[] { "Mobile number is already registered." });
        }

        var (userType, isAdmin) = PortalRoleConstants.ToUserType(model.Role);

        if (userType == UserTypeConstants.Provider)
        {
            if (string.IsNullOrWhiteSpace(model.Cnic))
                return (false, new[] { "CNIC is required for Provider role." });
            if (!model.CategoryUid.HasValue || model.CategoryUid.Value <= 0)
                return (false, new[] { "Category is required for Provider role." });

            var categoryExists = await _db.ServiceCategories
                .AnyAsync(c => c.Uid == model.CategoryUid.Value, cancellationToken);
            if (!categoryExists)
                return (false, new[] { "Selected category was not found." });
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new UsersLogin
            {
                MobileNo = mobile,
                PasswordHash = PasswordHasher.Hash(model.Password),
                UserType = userType,
                IsActive = model.IsActive,
                IsVerified = model.IsVerified,
                CreatedOn = DateTime.Now
            };

            _db.UsersLogins.Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            var fullName = model.FullName.Trim();
            if (userType == UserTypeConstants.Client)
            {
                _db.Clients.Add(new Client
                {
                    UserUid = user.Uid,
                    FullName = fullName,
                    Cnic = model.Cnic?.Trim(),
                    CreatedOn = DateTime.Now
                });
            }
            else if (userType == UserTypeConstants.Provider)
            {
                _db.Providers.Add(new Provider
                {
                    UserUid = user.Uid,
                    FullName = fullName,
                    Cnic = model.Cnic!.Trim(),
                    CategoryUid = model.CategoryUid!.Value,
                    CreatedOn = DateTime.Now,
                    IsAvailable = true
                });
            }
            else
            {
                _db.Staff.Add(new Staff
                {
                    UserUid = user.Uid,
                    FullName = fullName,
                    IsAdmin = isAdmin,
                    Designation = model.Role,
                    CreatedOn = DateTime.Now
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation("UsersLogin {Uid} created with role {Role}.", user.Uid, model.Role);
            return (true, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create UsersLogin.");
            return (false, new[] { "Failed to create user." });
        }
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(
        UserEditVm model,
        CancellationToken cancellationToken = default)
    {
        if (!PortalRoleConstants.IsValid(model.Role))
        {
            return (false, new[] { "Invalid role selected." });
        }

        var user = await _db.UsersLogins
            .Include(u => u.Client)
            .Include(u => u.Provider)
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Uid == model.Id, cancellationToken);

        if (user == null)
        {
            return (false, new[] { "User not found." });
        }

        var mobile = model.MobileNo.Trim();
        var mobileTaken = await _db.UsersLogins
            .AnyAsync(u => u.MobileNo == mobile && u.Uid != user.Uid, cancellationToken);
        if (mobileTaken)
        {
            return (false, new[] { "Mobile number is already registered." });
        }

        var (newUserType, isAdmin) = PortalRoleConstants.ToUserType(model.Role);
        var currentRole = PortalRoleConstants.FromUser(user.UserType, user.Staff?.IsAdmin == true);

        // Allow Staff ↔ Super Admin only; other UserType changes are blocked.
        var currentBase = PortalRoleConstants.ToUserType(currentRole).UserType;
        if (!string.Equals(currentBase, newUserType, StringComparison.OrdinalIgnoreCase))
        {
            return (false, new[] { "User type (Client / Provider / Staff) cannot be changed after creation. Delete and recreate, or use the Clients / Providers modules." });
        }

        if (newUserType == UserTypeConstants.Provider)
        {
            if (string.IsNullOrWhiteSpace(model.Cnic))
                return (false, new[] { "CNIC is required for Provider role." });
            if (!model.CategoryUid.HasValue || model.CategoryUid.Value <= 0)
                return (false, new[] { "Category is required for Provider role." });
        }

        user.MobileNo = mobile;
        user.IsActive = model.IsActive;
        user.IsVerified = model.IsVerified;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        }

        var fullName = model.FullName.Trim();
        if (user.Client != null)
        {
            user.Client.FullName = fullName;
            user.Client.Cnic = model.Cnic?.Trim();
        }
        else if (user.Provider != null)
        {
            user.Provider.FullName = fullName;
            user.Provider.Cnic = model.Cnic!.Trim();
            user.Provider.CategoryUid = model.CategoryUid!.Value;
        }
        else if (user.Staff != null)
        {
            user.Staff.FullName = fullName;
            user.Staff.IsAdmin = isAdmin;
            user.Staff.Designation = model.Role;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("UsersLogin {Uid} updated.", user.Uid);
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> DeleteUserAsync(
        int id,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (int.TryParse(currentUserId, out var currentId) && currentId == id)
        {
            return (false, new[] { "You cannot delete your own account." });
        }

        var user = await _db.UsersLogins
            .Include(u => u.Client)
            .Include(u => u.Provider)
            .Include(u => u.Staff)
            .FirstOrDefaultAsync(u => u.Uid == id, cancellationToken);

        if (user == null)
        {
            return (false, new[] { "User not found." });
        }

        if (user.Client != null)
        {
            var clientUid = user.Client.Uid;
            var hasAddresses = await _db.ClientAddresses.AnyAsync(a => a.ClientUid == clientUid, cancellationToken);
            var hasRequests = await _db.CustomerServiceRequests.AnyAsync(r => r.ClientUid == clientUid, cancellationToken);
            if (hasAddresses || hasRequests)
            {
                return (false, new[] { "Cannot delete: client has addresses or service requests. Remove those first." });
            }

            _db.Clients.Remove(user.Client);
        }

        if (user.Provider != null)
        {
            _db.Providers.Remove(user.Provider);
        }

        if (user.Staff != null)
        {
            _db.Staff.Remove(user.Staff);
        }

        _db.UsersLogins.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("UsersLogin {Uid} deleted.", id);
        return (true, Array.Empty<string>());
    }

    private static string? ResolveFullName(UsersLogin user) =>
        user.Staff?.FullName ?? user.Client?.FullName ?? user.Provider?.FullName;
}
