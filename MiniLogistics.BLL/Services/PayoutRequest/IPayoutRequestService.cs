using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.PayoutRequest;

namespace MiniLogistics.BLL.Services.PayoutRequest;

public interface IPayoutRequestService
{
    // Seller tạo yêu cầu rút tiền
    Task<PayoutRequestResponseDTO> CreateAsync(
        long userId,
        CreatePayoutRequestDTO request);

    // Seller xem payout của Shop mình
    Task<PagedResponseDTO<PayoutRequestResponseDTO>> GetMyAsync(
        long userId,
        long shopId,
        PayoutRequestPaginationRequestDTO request);

    // Admin xem tất cả payout
    Task<PagedResponseDTO<PayoutRequestResponseDTO>> GetAllAsync(
        PayoutRequestPaginationRequestDTO request);

    // Admin xem chi tiết
    Task<PayoutRequestResponseDTO?> GetByIdAsync(
        long payoutRequestId);

    // Admin duyệt
    Task<PayoutRequestResponseDTO> ApproveAsync(
        long adminUserId,
        long payoutRequestId);

    // Admin từ chối
    Task<PayoutRequestResponseDTO> RejectAsync(
        long adminUserId,
        long payoutRequestId);
}