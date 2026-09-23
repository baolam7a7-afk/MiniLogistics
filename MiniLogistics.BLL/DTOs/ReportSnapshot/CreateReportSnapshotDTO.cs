namespace MiniLogistics.BLL.DTOs.ReportSnapshot;

public class CreateReportSnapshotDTO
{
    public string Scope { get; set; } = null!;

    public long? ShopId { get; set; }

    public DateOnly DateFrom { get; set; }

    public DateOnly DateTo { get; set; }

    public string MetricsJson { get; set; } = null!;
}