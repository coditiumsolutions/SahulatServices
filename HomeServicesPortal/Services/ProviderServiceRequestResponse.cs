namespace HomeServicesPortal.Services;

/// <summary>Outcome when loading service requests for a provider.</summary>
public enum ProviderServiceRequestResult
{
    Success,
    ProviderNotFound,
    CategoryNotAssigned
}

/// <summary>Result payload for provider service-request lookup.</summary>
public sealed class ProviderServiceRequestResponse
{
    public ProviderServiceRequestResult Result { get; init; }

    public IReadOnlyList<Models.Api.ProviderServiceRequestApiDto> Items { get; init; } =
        Array.Empty<Models.Api.ProviderServiceRequestApiDto>();
}
