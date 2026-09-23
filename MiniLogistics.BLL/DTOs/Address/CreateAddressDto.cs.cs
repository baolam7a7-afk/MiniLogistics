using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Address;

public class CreateAddressDto
{
    [Required]
    [MaxLength(100)]
    public string ReceiverName { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string ReceiverPhone { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Line1 { get; set; } = null!;

    [MaxLength(255)]
    public string? Line2 { get; set; }

    [MaxLength(100)]
    public string? Ward { get; set; }

    [MaxLength(100)]
    public string? District { get; set; }

    [MaxLength(100)]
    public string? Province { get; set; }

    [MaxLength(10)]
    public string Country { get; set; } = "VN";

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; }
}