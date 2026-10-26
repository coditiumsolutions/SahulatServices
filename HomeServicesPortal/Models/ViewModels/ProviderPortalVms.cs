namespace HomeServicesPortal.Models.ViewModels;

public class ProviderHomeVm
{
    public string FullName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string AvailableFrom { get; set; } = "09:00";
    public string AvailableTo { get; set; } = "18:00";

    /// <summary>Bookings offered to this provider and waiting for a response.</summary>
    public int AvailableRequests { get; set; }
    /// <summary>Accepted plus In Progress.</summary>
    public int ActiveBookings { get; set; }
    public int CompletedToday { get; set; }
    public decimal AverageRating { get; set; }
    /// <summary>Null when the wallet could not be loaded.</summary>
    public decimal? WalletBalance { get; set; }
}

/// <summary>My Documents / first-time document upload. One page serves both.</summary>
public class ProviderDocumentsVm
{
    public bool HasDocuments { get; set; }
    public bool IsVerified { get; set; }
    public string? Remarks { get; set; }

    /// <summary>Image URLs with a cache-busting version, or null when that slot has no image yet.</summary>
    public string? ProfilePhotoUrl { get; set; }
    public string? CnicFrontUrl { get; set; }
    public string? CnicBackUrl { get; set; }
    public string? PoliceUrl { get; set; }

    /// <summary>First submission: nothing uploaded yet, so the three required images are needed.</summary>
    public bool IsOnboarding => !HasDocuments;
}

public class ProviderProfileVm
{
    public int ProviderUid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MobileNo { get; set; } = string.Empty;
    public string Cnic { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public int ExperienceYears { get; set; }
    public string? City { get; set; }
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? VerificationRemarks { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int JobsCompleted { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? PhotoUrl { get; set; }

    public bool IsOnline { get; set; }
    public string AvailableFrom { get; set; } = "09:00";
    public string AvailableTo { get; set; } = "18:00";

    public List<string> Categories { get; set; } = new();
    public string? PrimaryCategory { get; set; }
    public List<string> Zones { get; set; } = new();
    /// <summary>Selected service titles grouped by category name.</summary>
    public List<IGrouping<string, string>> Titles { get; set; } = new();
}

public class ProviderProfileEditVm
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Required")]
    [System.ComponentModel.DataAnnotations.StringLength(150)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a gender.")]
    public string? Gender { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Required")]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "Enter a valid 13-digit CNIC")]
    public string Cnic { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years.")]
    [System.ComponentModel.DataAnnotations.Display(Name = "Experience (years)")]
    public int? ExperienceYears { get; set; }

    public string? City { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Selected service zones (at most <see cref="MaxZones"/>).</summary>
    public List<string> Zones { get; set; } = new();

    public const int MaxZones = 2;

    public string MobileNo { get; set; } = string.Empty;
    public List<string> Cities { get; set; } = new();
    public List<string> ZoneOptions { get; set; } = new();
}

public class ProviderCategoryOptionVm
{
    public int Uid { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public bool Primary { get; set; }
}

public class ProviderTitleOptionVm
{
    public int Uid { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CategoryUid { get; set; }
    public bool Selected { get; set; }
}

public class ProviderCategoriesVm
{
    public const int MaxCategories = 3;

    /// <summary>Categories grouped by their parent service name.</summary>
    public List<IGrouping<string, ProviderCategoryOptionVm>> Services { get; set; } = new();
    public List<ProviderTitleOptionVm> Titles { get; set; } = new();
    public Dictionary<int, string> CategoryNames { get; set; } = new();
}

public class ProviderPendingVm
{
    public string FullName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public bool HasDocuments { get; set; }
}
