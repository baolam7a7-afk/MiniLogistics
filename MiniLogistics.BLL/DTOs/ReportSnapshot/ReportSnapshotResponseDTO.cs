namespace MiniLogistics.BLL.DTOs.ReportSnapshot;

public class ReportSnapshotResponseDTO
{
    public long Id { get; set; }

    public string Scope { get; set; } = null!;

    public long? ShopId { get; set; }

    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public string MetricsJson { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}