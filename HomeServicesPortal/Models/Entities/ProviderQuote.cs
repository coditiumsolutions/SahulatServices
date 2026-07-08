using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class ProviderQuote
{
    public int Uid { get; set; }

    public int RequestUid { get; set; }

    public int ProviderUid { get; set; }

    public decimal? QuoteAmount { get; set; }

    public int? EstimatedArrivalMinutes { get; set; }

    public decimal? DistanceKm { get; set; }

    public string? Remarks { get; set; }

    public DateTime? QuoteDate { get; set; }

    public virtual ProviderProfile ProviderU { get; set; } = null!;

    public virtual ServiceRequest RequestU { get; set; } = null!;
}
