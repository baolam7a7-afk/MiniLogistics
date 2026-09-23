namespace MiniLogistics.BLL.DTOs.SupportTicket;

public class CreateSupportTicketDTO
{
    public string Subject { get; set; } = null!;

    public long? OrderId { get; set; }
}