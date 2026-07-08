namespace HomeServicesPortal.DTOs;

public class LoginResponse
{
    public int UserId { get; set; }

    public int ProfileId { get; set; }

    public string UserType { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string MobileNo { get; set; } = string.Empty;
}
