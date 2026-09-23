namespace MiniLogistics.BLL.DTOs.Dispute;

public class CreateDisputeDTO
{
    public long OrderId { get; set; }

    public string Reason { get; set; } = null!;
}