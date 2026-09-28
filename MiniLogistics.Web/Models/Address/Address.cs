namespace MiniLogistics.Web.Models.Address;

public class Address
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string ReceiverName { get; set; } = string.Empty;

    public string ReceiverPhone { get; set; } = string.Empty;

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }

    public string? Ward { get; set; }

    public string? District { get; set; }

    public string? Province { get; set; }

    public string? Country { get; set; }

    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; }
}