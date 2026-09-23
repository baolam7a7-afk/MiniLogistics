namespace MiniLogistics.BLL.DTOs.SupportMessage;

public class CreateSupportMessageDTO
{
    public long TicketId { get; set; }

    public string Message { get; set; } = null!;
}