namespace HomeServicesPortal.Models.Api;

public class BookingMaterialItemApiDto
{
    public string ItemName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }
}

public class UpdateBookingMaterialItemsRequestDto
{
    public List<VerifyCompletionMaterialItemDto> MaterialItems { get; set; } = new();
}
