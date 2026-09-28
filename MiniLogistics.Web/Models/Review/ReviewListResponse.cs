using MiniLogistics.Web.Models.Review;

namespace MiniLogistics.Web.Models.Review;

public class ReviewListResponse
{
    public List<ReviewResponse> Items { get; set; } = new();

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}