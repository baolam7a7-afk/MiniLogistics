namespace MiniLogistics.BLL.DTOs.Common;

public class ErrorResponseDTO
{
    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public DateTime Timestamp { get; set; }
}