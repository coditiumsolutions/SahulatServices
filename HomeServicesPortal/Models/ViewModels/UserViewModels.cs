using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomeServicesPortal.Models.ViewModels;

public class UserListVm
{
    public List<UserListItemVm> Users { get; set; } = new();
    public string? Search { get; set; }
}

public class UserListItemVm
{
    public int Id { get; set; }
    public string MobileNo { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastLogin { get; set; }
}

public class UserCreateVm
{
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20)]
    [Display(Name = "Mobile No")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Verified")]
    public bool IsVerified { get; set; }

    [StringLength(20)]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [Display(Name = "Category")]
    public int? CategoryUid { get; set; }

    public List<string> AvailableRoles { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
}

public class UserEditVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(20)]
    [Display(Name = "Mobile No")]
    public string MobileNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm New Password")]
    public string? ConfirmNewPassword { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Verified")]
    public bool IsVerified { get; set; }

    [StringLength(20)]
    [Display(Name = "CNIC")]
    public string? Cnic { get; set; }

    [Display(Name = "Category")]
    public int? CategoryUid { get; set; }

    public List<string> AvailableRoles { get; set; } = new();
    public List<SelectListItem> Categories { get; set; } = new();
}

public class UserDetailsVm
{
    public int Id { get; set; }
    public string MobileNo { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? Cnic { get; set; }
    public string? CategoryName { get; set; }
}

public class UserDeleteVm
{
    public int Id { get; set; }
    public string MobileNo { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
}
