using HomeServicesPortal.Models.Entities;
using HomeServicesPortal.Models.ViewModels;
using HomeServicesPortal.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

public class CustomerService : ICustomerService
{
    private readonly IRepository<Customer> _repo;

    public CustomerService(IRepository<Customer> repo)
    {
        _repo = repo;
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

        var query = _repo.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.FullName.Contains(term) ||
                (c.MobileNo != null && c.MobileNo.Contains(term)) ||
                (c.Email != null && c.Email.Contains(term)) ||
                (c.Address != null && c.Address.Contains(term)));
        }

        query = sort switch
        {
            "email" => sortDir == "desc"
                ? query.OrderByDescending(c => c.Email)
                : query.OrderBy(c => c.Email),
            "mobile" => sortDir == "desc"
                ? query.OrderByDescending(c => c.MobileNo)
                : query.OrderBy(c => c.MobileNo),
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
                MobileNo = c.MobileNo,
                Email = c.Email,
                Address = c.Address,
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
        return await _repo.Query()
            .Where(c => c.Uid == id)
            .Select(c => new CustomerDetailsVm
            {
                Uid = c.Uid,
                FullName = c.FullName,
                MobileNo = c.MobileNo,
                Email = c.Email,
                Address = c.Address,
                CreatedOn = c.CreatedOn,
                ServiceRequestCount = c.ServiceRequests.Count,
                ReviewCount = c.Reviews.Count
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerFormVm?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        return new CustomerFormVm
        {
            Uid = entity.Uid,
            FullName = entity.FullName,
            MobileNo = entity.MobileNo,
            Email = entity.Email,
            Address = entity.Address
        };
    }

    public async Task<CustomerDeleteVm?> GetForDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _repo.Query()
            .Where(c => c.Uid == id)
            .Select(c => new CustomerDeleteVm
            {
                Uid = c.Uid,
                FullName = c.FullName,
                MobileNo = c.MobileNo,
                Email = c.Email
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(
        CustomerFormVm model,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var emailExists = await _repo.Query()
                .AnyAsync(c => c.Email == model.Email.Trim(), cancellationToken);

            if (emailExists)
            {
                return (false, "A customer with this email already exists.");
            }
        }

        var entity = new Customer
        {
            FullName = model.FullName.Trim(),
            MobileNo = model.MobileNo?.Trim(),
            Email = model.Email?.Trim(),
            Address = model.Address?.Trim(),
            CreatedOn = DateTime.Now
        };

        await _repo.AddAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        CustomerFormVm model,
        CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(model.Uid, cancellationToken);
        if (entity == null)
        {
            return (false, "Customer not found.");
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var duplicate = await _repo.Query()
                .AnyAsync(c => c.Email == model.Email.Trim() && c.Uid != model.Uid, cancellationToken);

            if (duplicate)
            {
                return (false, "A customer with this email already exists.");
            }
        }

        entity.FullName = model.FullName.Trim();
        entity.MobileNo = model.MobileNo?.Trim();
        entity.Email = model.Email?.Trim();
        entity.Address = model.Address?.Trim();

        await _repo.UpdateAsync(entity, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repo.GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return (false, "Customer not found.");
        }

        try
        {
            await _repo.DeleteAsync(entity, cancellationToken);
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Cannot delete this customer because they have linked service requests or reviews.");
        }
    }
}
