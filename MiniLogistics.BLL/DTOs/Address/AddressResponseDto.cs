namespace MiniLogistics.BLL.DTOs.Address;

public class AddressResponseDto
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public string ReceiverName { get; set; } = null!;

    public string ReceiverPhone { get; set; } = null!;

    public string Line1 { get; set; } = null!;

    public string? Line2 { get; set; }

    public string? Ward { get; set; }

    public string? District { get; set; }

    public string? Province { get; set; }

    public string Country { get; set; } = "VN";

    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}