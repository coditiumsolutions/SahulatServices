namespace HomeServicesPortal.Entities;

public class Client
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Cnic { get; set; }

    public string? Gender { get; set; }

    public DateTime CreatedOn { get; set; }

    public UsersLogin User { get; set; } = null!;
}
