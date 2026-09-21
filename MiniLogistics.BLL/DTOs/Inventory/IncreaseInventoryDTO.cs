using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Inventory;

public class IncreaseInventoryDTO
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}