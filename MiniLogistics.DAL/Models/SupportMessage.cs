using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class SupportMessage
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public long SenderUserId { get; set; }
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public SupportTicket Ticket { get; set; } = null!;
    public User SenderUser { get; set; } = null!;
}
