using System;
using System.Collections.Generic;

namespace MiniLogistics.DAL.Models;

public class ReviewReply
{
    public long Id { get; set; }
    public long ReviewId { get; set; }
    public long ShopId { get; set; }
    public long RepliedByUserId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public Review Review { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public User RepliedByUser { get; set; } = null!;
}
