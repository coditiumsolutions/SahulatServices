namespace HomeServicesPortal.Models.Api;

public class ClientDetailApiDto
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string MobileNo { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Cnic { get; set; }

    public string? Gender { get; set; }
}
