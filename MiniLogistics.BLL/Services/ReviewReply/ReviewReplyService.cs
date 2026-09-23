using MiniLogistics.BLL.DTOs.ReviewReply;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ReviewReplyModel =
    MiniLogistics.DAL.Models.ReviewReply;

namespace MiniLogistics.BLL.Services.ReviewReply;

public class ReviewReplyService : IReviewReplyService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReviewReplyService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<ReviewReplyResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateReviewReplyDTO request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        role = role.Trim().ToLowerInvariant();

        if (role != "admin" && role != "seller")
        {
            throw new ForbiddenException(
                "Chỉ admin hoặc seller được trả lời Review.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new BadRequestException(
                "Content không được để trống.");
        }

        var content = request.Content.Trim();

        // -----------------------------------------------------
        // Kiểm tra Review
        // -----------------------------------------------------

        var review =
            await _unitOfWork.Reviews.GetByIdAsync(
                request.ReviewId);

        if (review == null)
        {
            throw new NotFoundException(
                $"Review {request.ReviewId} không tồn tại.");
        }

        // -----------------------------------------------------
        // Kiểm tra Shop
        // -----------------------------------------------------

        var shop =
            await _unitOfWork.Shops.GetByIdAsync(
                request.ShopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {request.ShopId} không tồn tại.");
        }

        // -----------------------------------------------------
        // Seller chỉ được trả lời review của Shop mình
        // -----------------------------------------------------

        if (role == "seller" &&
            shop.OwnerUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền trả lời Review của Shop này.");
        }

        // -----------------------------------------------------
        // Kiểm tra Product
        // -----------------------------------------------------

        var product =
            await _unitOfWork.Products.GetByIdAsync(
                review.ProductId);

        if (product == null)
        {
            throw new NotFoundException(
                $"Product {review.ProductId} không tồn tại.");
        }

        // -----------------------------------------------------
        // Review phải thuộc Product của Shop
        // -----------------------------------------------------

        if (product.ShopId != request.ShopId)
        {
            throw new BadRequestException(
                "Review không thuộc Product của Shop này.");
        }

        // -----------------------------------------------------
        // Một Review chỉ có một Reply
        // -----------------------------------------------------

        var existingReplies =
            await _unitOfWork.ReviewReplies.FindAsync(
                x => x.ReviewId == request.ReviewId);

        if (existingReplies.Any())
        {
            throw new BadRequestException(
                "Review này đã có câu trả lời.");
        }

        // -----------------------------------------------------
        // Tạo Reply
        // -----------------------------------------------------

        var reply = new ReviewReplyModel
        {
            ReviewId = request.ReviewId,
            ShopId = request.ShopId,
            RepliedByUserId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ReviewReplies.AddAsync(reply);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(reply);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<ReviewReplyResponseDTO?> GetByIdAsync(
        long id)
    {
        var reply =
            await _unitOfWork.ReviewReplies.GetByIdAsync(id);

        if (reply == null)
            return null;

        return MapToResponse(reply);
    }


    // =========================================================
    // GET BY REVIEW ID
    // =========================================================

    public async Task<ReviewReplyResponseDTO?> GetByReviewIdAsync(
        long reviewId)
    {
        var replies =
            await _unitOfWork.ReviewReplies.FindAsync(
                x => x.ReviewId == reviewId);

        var reply = replies.FirstOrDefault();

        if (reply == null)
            return null;

        return MapToResponse(reply);
    }


    // =========================================================
    // GET BY SHOP
    // =========================================================

    public async Task<IEnumerable<ReviewReplyResponseDTO>>
        GetByShopIdAsync(long shopId)
    {
        var shop =
            await _unitOfWork.Shops.GetByIdAsync(shopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }

        var replies =
            await _unitOfWork.ReviewReplies.FindAsync(
                x => x.ShopId == shopId);

        return replies
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<ReviewReplyResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateReviewReplyDTO request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        role = role.Trim().ToLowerInvariant();

        if (role != "admin" && role != "seller")
        {
            throw new ForbiddenException(
                "Chỉ admin hoặc seller được sửa Reply.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new BadRequestException(
                "Content không được để trống.");
        }

        var reply =
            await _unitOfWork.ReviewReplies.GetByIdAsync(id);

        if (reply == null)
        {
            throw new NotFoundException(
                $"ReviewReply {id} không tồn tại.");
        }

        // Seller chỉ được sửa Reply do chính mình tạo
        if (role == "seller" &&
            reply.RepliedByUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền sửa Reply này.");
        }

        reply.Content =
            request.Content.Trim();

        _unitOfWork.ReviewReplies.Update(reply);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(reply);
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task DeleteAsync(
        long userId,
        string role,
        long id)
    {
        role = role.Trim().ToLowerInvariant();

        if (role != "admin" && role != "seller")
        {
            throw new ForbiddenException(
                "Chỉ admin hoặc seller được xóa Reply.");
        }

        var reply =
            await _unitOfWork.ReviewReplies.GetByIdAsync(id);

        if (reply == null)
        {
            throw new NotFoundException(
                $"ReviewReply {id} không tồn tại.");
        }

        // Seller chỉ được xóa Reply của mình
        if (role == "seller" &&
            reply.RepliedByUserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa Reply này.");
        }

        _unitOfWork.ReviewReplies.Delete(reply);

        await _unitOfWork.SaveChangesAsync();
    }


    // =========================================================
    // MAPPING
    // =========================================================

    private ReviewReplyResponseDTO MapToResponse(
        ReviewReplyModel reply)
    {
        return new ReviewReplyResponseDTO
        {
            Id = reply.Id,
            ReviewId = reply.ReviewId,
            ShopId = reply.ShopId,
            RepliedByUserId = reply.RepliedByUserId,
            Content = reply.Content,
            CreatedAt = reply.CreatedAt
        };
    }
}