namespace HomeServicesPortal.Entities;

public class Client
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Cnic { get; set; }

    public string? Gender { get; set; }

    public string? CustomerAlert { get; set; }

    public string? Comments { get; set; }

    public string? City { get; set; }

    public string? Location { get; set; }

    public DateTime CreatedOn { get; set; }

    public UsersLogin User { get; set; } = null!;
}
