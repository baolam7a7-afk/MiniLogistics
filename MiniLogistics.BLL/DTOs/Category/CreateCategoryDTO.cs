namespace MiniLogistics.BLL.DTOs.Category;

public class CreateCategoryDTO
{
    public long? ParentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}