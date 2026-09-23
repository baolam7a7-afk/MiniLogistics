namespace MiniLogistics.BLL.DTOs.Dispute;

public class DisputeResponseDTO
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long RaisedByUserId { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public long? HandledByUserId { get; set; }

    public DateTime? HandledAt { get; set; }
}