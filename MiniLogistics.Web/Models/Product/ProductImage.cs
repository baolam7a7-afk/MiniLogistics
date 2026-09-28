namespace MiniLogistics.Web.Models.Product;

public class ProductImage
{
    public long Id { get; set; }

    public long ProductId { get; set; }

    public string Url { get; set; } = string.Empty;
}
