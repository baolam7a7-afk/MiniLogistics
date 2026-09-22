namespace MiniLogistics.BLL.DTOs.Order;

public class OrderItemResponseDTO
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public long VariantId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string VariantName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}