namespace MiniLogistics.BLL.DTOs.DisputeMessage;

public class DisputeMessageResponseDTO
{
    public long Id { get; set; }

    public long DisputeId { get; set; }

    public long SenderUserId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}