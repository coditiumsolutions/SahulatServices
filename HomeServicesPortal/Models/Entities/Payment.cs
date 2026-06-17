using System;
using System.Collections.Generic;

namespace HomeServicesPortal.Models.Entities;

public partial class Payment
{
    public int Uid { get; set; }

    public int BookingUid { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TransactionNo { get; set; }

    public DateTime? PaymentDate { get; set; }

    public virtual Booking Bookin { get; set; } = null!;
}
