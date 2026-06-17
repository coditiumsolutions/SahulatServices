using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class Customer
{
    public int Uid { get; set; }

    public string FullName { get; set; } = null!;

    public string? MobileNo { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTime? CreatedOn { get; set; }

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
