namespace MiniLogistics.BLL.DTOs.SupportMessage;

public class SupportMessageResponseDTO
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public long SenderUserId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}