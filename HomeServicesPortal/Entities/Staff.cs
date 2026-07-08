namespace HomeServicesPortal.Entities;

public class Staff
{
    public int Uid { get; set; }

    public int UserUid { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Designation { get; set; }

    public string? Department { get; set; }

    public bool IsAdmin { get; set; }

    public DateTime CreatedOn { get; set; }

    public UsersLogin User { get; set; } = null!;
}
