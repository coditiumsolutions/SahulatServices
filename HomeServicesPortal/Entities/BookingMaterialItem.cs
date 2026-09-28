namespace HomeServicesPortal.Entities;

/// <summary>
/// One line item of a booking's material-cost breakdown (e.g. "light bulb", qty 2, unit price
/// 200). ServiceBookings.MaterialAmount is not a stored column — the material total is the sum
/// of these rows, computed at query time.
/// </summary>
public class BookingMaterialItem
{
    public int Uid { get; set; }

    public int BookingUid { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    /// <summary>Quantity * UnitPrice, stored (not computed) for historical stability.</summary>
    public decimal Amount { get; set; }

    public DateTime CreatedOn { get; set; }

    public ServiceBooking Booking { get; set; } = null!;
}
