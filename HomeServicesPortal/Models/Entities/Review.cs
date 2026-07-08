using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class Review
{
    public int Uid { get; set; }

    public int BookingUid { get; set; }

    public int CustomerUid { get; set; }

    public int ProviderUid { get; set; }

    public int? Rating { get; set; }

    public string? ReviewText { get; set; }

    public DateTime? ReviewDate { get; set; }

    public virtual Booking Bookin { get; set; } = null!;

    public virtual Customer CustomerU { get; set; } = null!;

    public virtual ProviderProfile ProviderU { get; set; } = null!;
}
