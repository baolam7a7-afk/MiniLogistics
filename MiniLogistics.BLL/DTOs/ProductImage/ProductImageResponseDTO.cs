namespace MiniLogistics.BLL.DTOs.ProductImage;

public class ProductImageResponseDTO
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public string Url { get; set; } = null!;
}