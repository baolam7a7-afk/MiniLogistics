namespace MiniLogistics.BLL.DTOs.SupportTicket;

public class SupportTicketResponseDTO
{
    public long Id { get; set; }

    public string Subject { get; set; } = null!;

    public string Status { get; set; } = null!;

    public long CreatedByUserId { get; set; }

    public long? OrderId { get; set; }

    public DateTime CreatedAt { get; set; }
}