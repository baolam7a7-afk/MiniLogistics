using MiniLogistics.BLL.DTOs.Review;

namespace MiniLogistics.BLL.Services.Review;

public interface IReviewService
{
    Task<ReviewResponseDTO> CreateAsync(
        long customerId,
        CreateReviewDTO request);

    Task<ReviewResponseDTO> GetByIdAsync(
        long reviewId);

    Task<IEnumerable<ReviewResponseDTO>> GetByProductIdAsync(
        long productId);

    Task<IEnumerable<ReviewResponseDTO>> GetMyReviewsAsync(
        long customerId);

    Task<ReviewResponseDTO> UpdateAsync(
        long customerId,
        long reviewId,
        UpdateReviewDTO request);

    Task DeleteAsync(
        long customerId,
        long reviewId);
}