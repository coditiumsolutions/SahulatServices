namespace HomeServicesPortal.Entities;

public class ClientAddress
{
    public int Uid { get; set; }

    public int ClientUid { get; set; }

    public string AddressTitle { get; set; } = string.Empty;

    public string FullAddress { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public Client Client { get; set; } = null!;

    public ICollection<CustomerServiceRequest> ServiceRequests { get; set; } = new List<CustomerServiceRequest>();
}
