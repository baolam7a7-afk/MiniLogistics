using MiniLogistics.BLL.DTOs.ReviewReply;

namespace MiniLogistics.BLL.Services.ReviewReply;

public interface IReviewReplyService
{
    Task<ReviewReplyResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateReviewReplyDTO request);

    Task<ReviewReplyResponseDTO?> GetByIdAsync(
        long id);

    Task<ReviewReplyResponseDTO?> GetByReviewIdAsync(
        long reviewId);

    Task<IEnumerable<ReviewReplyResponseDTO>> GetByShopIdAsync(
        long shopId);

    Task<ReviewReplyResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateReviewReplyDTO request);

    Task DeleteAsync(
        long userId,
        string role,
        long id);
}