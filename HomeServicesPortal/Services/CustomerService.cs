using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Helpers;
using HomeServicesPortal.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerListVm> GetListAsync(
        string? search,
        string? sort,
        string? sortDir,
        int page,
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 10;
        page = page < 1 ? 1 : page;
        sort = string.IsNullOrWhiteSpace(sort) ? "name" : sort.ToLowerInvariant();
        sortDir = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var query = _db.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.FullName.Contains(term) ||
                c.User.MobileNo.Contains(term) ||
                (c.Cnic != null && c.Cnic.Contains(term)) ||
                (c.Gender != null && c.Gender.Contains(term)));
        }

        query = sort switch
        {
            "mobile" => sortDir == "desc"
                ? query.OrderByDescending(c => c.User.MobileNo)
                : query.OrderBy(c => c.User.MobileNo),
            "date" => sortDir == "desc"
                ? query.OrderByDescending(c => c.CreatedOn)
                : query.OrderBy(c => c.CreatedOn),
            _ => sortDir == "desc"
                ? query.OrderByDescending(c => c.FullName)
                : query.OrderBy(c => c.FullName)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerItemVm
            {
                Uid = c.Uid,
                FullName = c.FullName,
                MobileNo = c.User.MobileNo,
                Cnic = c.Cnic,
                Gender = c.Gender,
                CreatedOn = c.CreatedOn
            })
            .ToListAsync(cancellationToken);

        return new CustomerListVm
        {
            Items = items,
            Search = search,
            Sort = sort,
            SortDir = sortDir,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CustomerDetailsVm?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Clients
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new CustomerDetailsVm
            {
                Uid = c.Uid,
                FullName = c.FullName,
                MobileNo = c.User.MobileNo,
                Cnic = c.Cnic,
                Gender = c.Gender,
                CreatedOn = c.CreatedOn,
                ServiceRequestCount = _db.CustomerServiceRequests.Count(r => r.ClientUid == c.Uid),
                AddressCount = _db.ClientAddresses.Count(a => a.ClientUid == c.Uid)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Clients
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new CustomerFormVm
            {
                Uid = c.Uid,
                FullName = c.FullName,
                MobileNo = c.User.MobileNo,
                Cnic = c.Cnic,
                Gender = c.Gender
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Clients
            .AsNoTracking()
            .Where(c => c.Uid == id)
            .Select(c => new CustomerDeleteVm
            {
                Uid = c.Uid,
                FullName = c.FullName,
                MobileNo = c.User.MobileNo,
                Cnic = c.Cnic
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        CustomerFormVm model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.MobileNo))
        {
            return (false, "Mobile number is required.");
        }

        var mobile = model.MobileNo.Trim();
        var mobileExists = await _db.UsersLogins
            .AnyAsync(u => u.MobileNo == mobile, cancellationToken);

        if (mobileExists)
        {
            return (false, "A user with this mobile number already exists.");
        }

        var user = new UsersLogin
        {
            MobileNo = mobile,
            PasswordHash = PasswordHasher.Hash(Guid.NewGuid().ToString("N")[..8]),
            UserType = UserTypeConstants.Client,
            IsActive = true,
            IsVerified = false,
            CreatedOn = DateTime.Now
        };

        _db.UsersLogins.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        _db.Clients.Add(new Client
        {
            UserUid = user.Uid,
            FullName = model.FullName.Trim(),
            Cnic = model.Cnic?.Trim(),
            Gender = model.Gender?.Trim(),
            CreatedOn = DateTime.Now
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        CustomerFormVm model,
        CancellationToken cancellationToken = default)
    {
        var client = await _db.Clients
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Uid == model.Uid, cancellationToken);

        if (client == null)
        {
            return (false, "Client not found.");
        }

        if (string.IsNullOrWhiteSpace(model.MobileNo))
        {
            return (false, "Mobile number is required.");
        }

        var mobile = model.MobileNo.Trim();
        var mobileTaken = await _db.UsersLogins
            .AnyAsync(u => u.MobileNo == mobile && u.Uid != client.UserUid, cancellationToken);

        if (mobileTaken)
        {
            return (false, "A user with this mobile number already exists.");
        }

        client.FullName = model.FullName.Trim();
        client.Cnic = model.Cnic?.Trim();
        client.Gender = model.Gender?.Trim();
        client.User.MobileNo = mobile;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await _db.Clients
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Uid == id, cancellationToken);

        if (client == null)
        {
            return (false, "Client not found.");
        }

        var hasRequests = await _db.CustomerServiceRequests
            .AnyAsync(r => r.ClientUid == id, cancellationToken);

        var hasAddresses = await _db.ClientAddresses
            .AnyAsync(a => a.ClientUid == id, cancellationToken);

        if (hasRequests || hasAddresses)
        {
            return (false, "Cannot delete this client because they have linked addresses or service requests.");
        }

        var user = client.User;
        _db.Clients.Remove(client);
        _db.UsersLogins.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
