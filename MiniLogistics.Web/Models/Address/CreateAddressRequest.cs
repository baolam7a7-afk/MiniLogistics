namespace MiniLogistics.Web.Models.Address;

public class CreateAddressRequest
{
    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string Line1 { get; set; } = string.Empty;

    public string Line2 { get; set; } = string.Empty;

    public string Ward { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}