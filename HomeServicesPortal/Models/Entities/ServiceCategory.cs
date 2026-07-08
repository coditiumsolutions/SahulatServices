using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class ServiceCategory
{
    public int Uid { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedOn { get; set; }

    public virtual ICollection<ProviderProfile> ProviderProfiles { get; set; } = new List<ProviderProfile>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
