namespace MiniLogistics.BLL.DTOs.Category;

public class CategoryResponseDTO
{
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}