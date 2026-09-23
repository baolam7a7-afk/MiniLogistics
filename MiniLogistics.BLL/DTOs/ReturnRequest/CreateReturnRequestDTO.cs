using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.ReturnRequest;

public class CreateReturnRequestDTO
{
    [Required]
    public long OrderId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = null!;

    [MaxLength(2000)]
    public string? Description { get; set; }
}