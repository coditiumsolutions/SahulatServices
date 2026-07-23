using HomeServicesPortal.Entities;

namespace HomeServicesPortal.Interfaces;

/// <summary>Data access for dbo.ProviderDocuments (one row per provider).</summary>
public interface IProviderDocumentRepository
{
    Task<bool> ProviderExistsAsync(int providerUid, CancellationToken cancellationToken = default);

    Task<ProviderDocument?> GetByProviderUidAsync(int providerUid, CancellationToken cancellationToken = default);

    Task<ProviderDocument> AddAsync(ProviderDocument document, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProviderDocument document, CancellationToken cancellationToken = default);

    Task DeleteAsync(ProviderDocument document, CancellationToken cancellationToken = default);
}
