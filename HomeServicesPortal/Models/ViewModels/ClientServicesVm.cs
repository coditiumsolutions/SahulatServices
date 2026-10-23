namespace HomeServicesPortal.Models.ViewModels;

public class ClientServicesVm
{
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<ClientServiceCardVm> Services { get; set; } = Array.Empty<ClientServiceCardVm>();
}

public class ClientServiceCardVm
{
    public int Uid { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IconClass { get; set; } = "fa-solid fa-screwdriver-wrench";
}

public class ClientServiceCategoriesVm
{
    public int ServiceUid { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceDescription { get; set; }
    public IReadOnlyList<ClientCategoryCardVm> Categories { get; set; } = Array.Empty<ClientCategoryCardVm>();
}

public class ClientCategoryCardVm
{
    public int Uid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ClientDashboardVm
{
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<ClientServiceCardVm> Services { get; set; } = Array.Empty<ClientServiceCardVm>();
    public IReadOnlyList<ClientSearchCategoryVm> Categories { get; set; } = Array.Empty<ClientSearchCategoryVm>();
}

/// <summary>A category with its parent service, used by the dashboard search and featured row.</summary>
public class ClientSearchCategoryVm
{
    public int Uid { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ServiceUid { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}
