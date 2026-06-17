using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class ProviderAvailability
{
    public int Uid { get; set; }

    public int ProviderUid { get; set; }

    public bool? IsOnline { get; set; }

    public TimeOnly? AvailableFrom { get; set; }

    public TimeOnly? AvailableTo { get; set; }

    public virtual ServiceProvider ProviderU { get; set; } = null!;
}
