namespace HomeServicesPortal.DTOs;

public class RegistrationResponse
{
    public int UserId { get; set; }

    public int ProfileId { get; set; }

    public string UserType { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string MobileNo { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }
}
