using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class BookingTracking
{
    public int Uid { get; set; }

    public int BookingUid { get; set; }

    public string? Status { get; set; }

    public string? Remarks { get; set; }

    public DateTime? StatusDate { get; set; }

    public virtual Booking Bookin { get; set; } = null!;
}
