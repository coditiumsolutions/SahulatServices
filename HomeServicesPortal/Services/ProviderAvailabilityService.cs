using HomeServicesPortal.Models.Api;
using HomeServicesPortal.Data;
using HomeServicesPortal.Entities;
using HomeServicesPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

using HomeServicesPortal.Helpers;

namespace HomeServicesPortal.Services;

public class ProviderAvailabilityService : IProviderAvailabilityService
{
    private readonly AppDbContext _appDbContext;

    public ProviderAvailabilityService(
        AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<(bool Success, string? Error, ProviderAvailableStatusApiDto? Data)> GetProviderAvailabilityStatusAsync(
        int providerUid,
        CancellationToken cancellationToken = default)
    {
        var provider = await _appDbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Uid == providerUid, cancellationToken);

        if (provider == null)
        {
            return (false, "Provider not found.", null);
        }

        return (true, null, MapProviderAvailability(provider));
    }

    public async Task<(bool Success, string? Error, ProviderAvailableStatusApiDto? Data)> SaveProviderAvailabilityStatusAsync(
        SetProviderAvailableStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var provider = await _appDbContext.Providers
            .FirstOrDefaultAsync(p => p.Uid == request.ProviderUid, cancellationToken);

        if (provider == null)
        {
            return (false, "Provider not found.", null);
        }

        provider.IsAvailable = request.IsOnline;

        if (request.IsOnline)
        {
            if (!request.TryResolveTiming(out var availableFrom, out var availableTo, out var timingError))
            {
                return (false, timingError ?? "AvailableFrom and AvailableTo are required when provider is online.", null);
            }

            provider.AvailableTiming = BuildAvailableTiming(availableFrom, availableTo);
        }
        else
        {
            provider.AvailableTiming = null;
        }

        await _appDbContext.SaveChangesAsync(cancellationToken);

        return (true, null, MapProviderAvailability(provider));
    }

    private static ProviderAvailableStatusApiDto MapProviderAvailability(Provider provider)
    {
        var (availableFrom, availableTo) = ParseAvailableTiming(provider.AvailableTiming);

        return new ProviderAvailableStatusApiDto
        {
            Uid = provider.Uid,
            ProviderUid = provider.Uid,
            IsOnline = provider.IsAvailable,
            AvailableFrom = availableFrom,
            AvailableTo = availableTo,
            AvailableTiming = provider.AvailableTiming
        };
    }

    private static string BuildAvailableTiming(string? availableFrom, string? availableTo) =>
        $"{availableFrom!.Trim()} - {availableTo!.Trim()}";

    private static (string? AvailableFrom, string? AvailableTo) ParseAvailableTiming(string? availableTiming)
    {
        if (string.IsNullOrWhiteSpace(availableTiming))
        {
            return (null, null);
        }

        var parts = availableTiming.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 ? (parts[0], parts[1]) : (null, null);
    }
}
