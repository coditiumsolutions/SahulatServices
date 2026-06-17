using HomeServicesPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Repositories;

public class EfRepository<T> : IRepository<T> where T : class
{
    private readonly SahulatAppDbContext _context;
    private readonly DbSet<T> _set;

    public EfRepository(SahulatAppDbContext context)
    {
        _context = context;
        _set = context.Set<T>();
    }

    public IQueryable<T> Query() => _set.AsNoTracking();

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await _set.FindAsync(new[] { id }, cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _set.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _set.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

