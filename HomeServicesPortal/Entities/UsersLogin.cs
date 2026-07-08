namespace HomeServicesPortal.Entities;

/// <summary>Core authentication record for all user types.</summary>
public class UsersLogin
{
    public int Uid { get; set; }

    public string MobileNo { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string UserType { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsVerified { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? LastLogin { get; set; }

    public Client? Client { get; set; }

    public Provider? Provider { get; set; }

    public Staff? Staff { get; set; }
}
