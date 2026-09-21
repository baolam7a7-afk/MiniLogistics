using System.ComponentModel.DataAnnotations;

namespace MiniLogistics.BLL.DTOs.Inventory;

public class AdjustInventoryDTO
{
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
}