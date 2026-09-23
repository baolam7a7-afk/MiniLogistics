using MiniLogistics.BLL.DTOs.Review;
using MiniLogistics.DAL.UnitOfWork;
using MiniLogistics.BLL.Exceptions;
using ReviewModel = MiniLogistics.DAL.Models.Review;

namespace MiniLogistics.BLL.Services.Review;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReviewResponseDTO> CreateAsync(
        long customerId,
        CreateReviewDTO request)
    {
        if (request == null)
            throw new BadRequestException(
                "Review request không được null.");

        // Rating phải từ 1 đến 5
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new BadRequestException(
                "Rating phải nằm trong khoảng từ 1 đến 5.");
        }

        var order =
            await _unitOfWork.Orders.GetByIdAsync(request.OrderId);

        if (order == null)
            throw new NotFoundException(
                $"Không tìm thấy Order với Id = {request.OrderId}.");

        // Chỉ customer sở hữu Order mới được đánh giá
        if (order.CustomerId != customerId)
            throw new ForbiddenException(
                "Bạn không có quyền đánh giá Order này.");

        // Chỉ được review đơn đã giao
        if (!string.Equals(
                order.Status,
                "delivered",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                "Chỉ có thể đánh giá đơn hàng đã giao thành công.");
        }

        var orderItem =
            await _unitOfWork.OrderItems.GetByIdAsync(
                request.OrderItemId);

        if (orderItem == null)
            throw new NotFoundException(
                $"Không tìm thấy OrderItem với Id = {request.OrderItemId}.");

        // OrderItem phải thuộc Order
        if (orderItem.OrderId != request.OrderId)
            throw new BadRequestException(
                "OrderItem không thuộc Order được gửi lên.");

        // Product phải đúng với Product của OrderItem
        if (orderItem.ProductId != request.ProductId)
            throw new BadRequestException(
                "Product không khớp với Product của OrderItem.");

        var product =
            await _unitOfWork.Products.GetByIdAsync(
                request.ProductId);

        if (product == null)
            throw new NotFoundException(
                $"Không tìm thấy Product với Id = {request.ProductId}.");

        // Mỗi OrderItem chỉ được đánh giá một lần
        var existingReview =
            await _unitOfWork.Reviews.FindAsync(
                r => r.OrderItemId == request.OrderItemId);

        if (existingReview.Any())
        {
            throw new BadRequestException(
                "OrderItem này đã được đánh giá.");
        }

        var review = new ReviewModel
        {
            OrderId = request.OrderId,
            OrderItemId = request.OrderItemId,
            ProductId = request.ProductId,
            CustomerId = customerId,
            Rating = request.Rating,

            Content =
                string.IsNullOrWhiteSpace(request.Content)
                    ? null
                    : request.Content.Trim(),

            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        await _unitOfWork.Reviews.AddAsync(review);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(review);
    }

    public async Task<ReviewResponseDTO> GetByIdAsync(
        long reviewId)
    {
        var review =
            await _unitOfWork.Reviews.GetByIdAsync(reviewId);

        if (review == null)
            throw new NotFoundException(
                $"Không tìm thấy Review với Id = {reviewId}.");

        return MapToResponse(review);
    }

    public async Task<IEnumerable<ReviewResponseDTO>>
        GetByProductIdAsync(long productId)
    {
        var product =
            await _unitOfWork.Products.GetByIdAsync(productId);

        if (product == null)
            throw new NotFoundException(
                $"Không tìm thấy Product với Id = {productId}.");

        var reviews =
            await _unitOfWork.Reviews.FindAsync(
                r => r.ProductId == productId);

        return reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<IEnumerable<ReviewResponseDTO>>
        GetMyReviewsAsync(long customerId)
    {
        var reviews =
            await _unitOfWork.Reviews.FindAsync(
                r => r.CustomerId == customerId);

        return reviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<ReviewResponseDTO> UpdateAsync(
        long customerId,
        long reviewId,
        UpdateReviewDTO request)
    {
        if (request == null)
            throw new BadRequestException(
                "Review request không được null.");

        // Rating phải từ 1 đến 5
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new BadRequestException(
                "Rating phải nằm trong khoảng từ 1 đến 5.");
        }

        var review =
            await _unitOfWork.Reviews.GetByIdAsync(reviewId);

        if (review == null)
            throw new NotFoundException(
                $"Không tìm thấy Review với Id = {reviewId}.");

        // Chỉ customer sở hữu Review mới được sửa
        if (review.CustomerId != customerId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền sửa Review này.");
        }

        review.Rating = request.Rating;

        review.Content =
            string.IsNullOrWhiteSpace(request.Content)
                ? null
                : request.Content.Trim();

        review.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Reviews.Update(review);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(review);
    }

    public async Task DeleteAsync(
        long customerId,
        long reviewId)
    {
        var review =
            await _unitOfWork.Reviews.GetByIdAsync(reviewId);

        if (review == null)
            throw new NotFoundException(
                $"Không tìm thấy Review với Id = {reviewId}.");

        // Chỉ customer sở hữu Review mới được xóa
        if (review.CustomerId != customerId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa Review này.");
        }

        _unitOfWork.Reviews.Delete(review);

        await _unitOfWork.SaveChangesAsync();
    }

    private ReviewResponseDTO MapToResponse(
        ReviewModel review)
    {
        return new ReviewResponseDTO
        {
            Id = review.Id,
            OrderId = review.OrderId,
            OrderItemId = review.OrderItemId,
            ProductId = review.ProductId,
            CustomerId = review.CustomerId,
            Rating = review.Rating,
            Content = review.Content,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}