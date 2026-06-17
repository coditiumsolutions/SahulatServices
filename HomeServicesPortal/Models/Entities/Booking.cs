using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class Booking
{
    public int Uid { get; set; }

    public int RequestUid { get; set; }

    public int ProviderUid { get; set; }

    public DateTime? BookingDate { get; set; }

    public decimal? FinalAmount { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<BookingTracking> BookingTrackings { get; set; } = new List<BookingTracking>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ServiceProvider ProviderU { get; set; } = null!;

    public virtual ServiceRequest RequestU { get; set; } = null!;

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
