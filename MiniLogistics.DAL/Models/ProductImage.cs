using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class ProductImage
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; }
    public Product Product { get; set; } = null!;
}
