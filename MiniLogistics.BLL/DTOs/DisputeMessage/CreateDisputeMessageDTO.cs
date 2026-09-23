namespace MiniLogistics.BLL.DTOs.DisputeMessage;

public class CreateDisputeMessageDTO
{
    public long DisputeId { get; set; }

    public string Message { get; set; } = null!;
}