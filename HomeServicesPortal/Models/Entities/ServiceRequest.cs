using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class ServiceRequest
{
    public int Uid { get; set; }

    public int CustomerUid { get; set; }

    public int CategoryUid { get; set; }

    public string? ServiceAddress { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? ProblemDescription { get; set; }

    public DateTime? RequestDate { get; set; }

    public string? Status { get; set; }

    public virtual ServiceCategory CategoryU { get; set; } = null!;

    public virtual Customer CustomerU { get; set; } = null!;

    public virtual ICollection<ProviderQuote> ProviderQuotes { get; set; } = new List<ProviderQuote>();
}
