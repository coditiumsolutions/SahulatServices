using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class ProviderLocation
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ProviderProfile ProviderU { get; set; } = null!;
}
