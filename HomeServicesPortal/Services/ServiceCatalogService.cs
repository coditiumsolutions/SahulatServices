using HomeServicesPortal.Data;
using HomeServicesPortal.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace HomeServicesPortal.Services;

/// <summary>Public API access to the Services catalog (parent of categories).</summary>
public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _db;

    public ServiceCatalogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ServiceApiDto>> GetActiveServicesForApiAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.ServiceName)
            .Select(s => new ServiceApiDto
            {
                Id = s.Uid,
                Name = s.ServiceName,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                CreatedOn = s.CreatedOn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceApiDto?> GetActiveServiceForApiAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _db.Services
            .AsNoTracking()
            .Where(s => s.Uid == id && s.IsActive)
            .Select(s => new ServiceApiDto
            {
                Id = s.Uid,
                Name = s.ServiceName,
                Description = s.Description,
                DisplayOrder = s.DisplayOrder,
                CreatedOn = s.CreatedOn
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
