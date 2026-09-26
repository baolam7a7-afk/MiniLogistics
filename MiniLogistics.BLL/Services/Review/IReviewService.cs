using MiniLogistics.BLL.DTOs.Review;
using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.Services.Review;

public interface IReviewService
{
    Task<ReviewResponseDTO> CreateAsync(
        long customerId,
        CreateReviewDTO request);

    Task<ReviewResponseDTO> GetByIdAsync(
        long reviewId);

    Task<PagedResponseDTO<ReviewResponseDTO>> GetByProductIdAsync(
        long productId,
        ReviewPaginationRequestDTO request);

    Task<PagedResponseDTO<ReviewResponseDTO>> GetMyReviewsAsync(
        long customerId,
        ReviewPaginationRequestDTO request);

    Task<ReviewResponseDTO> UpdateAsync(
        long customerId,
        long reviewId,
        UpdateReviewDTO request);

    Task DeleteAsync(
        long customerId,
        long reviewId);
}