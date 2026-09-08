namespace HomeServicesPortal.DTOs;

public class LoginResponse
{
    public int UserId { get; set; }

    public int ProfileId { get; set; }

    public string UserType { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string MobileNo { get; set; } = string.Empty;

    /// <summary>Clients.UID for this account, if a client profile exists — present regardless of UserType, so a Provider account can still submit customer service requests.</summary>
    public int? ClientId { get; set; }

    /// <summary>Providers.UID for this account, if a provider profile exists.</summary>
    public int? ProviderId { get; set; }
}
