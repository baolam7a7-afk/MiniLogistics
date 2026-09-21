using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Inventory;

public class DecreaseInventoryDTO
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}