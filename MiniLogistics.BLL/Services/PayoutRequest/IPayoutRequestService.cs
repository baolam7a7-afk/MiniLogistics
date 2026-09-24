using MiniLogistics.BLL.DTOs.PayoutRequest;

namespace MiniLogistics.BLL.Services.PayoutRequest;

public interface IPayoutRequestService
{
    // Seller tạo yêu cầu rút tiền
    Task<PayoutRequestResponseDTO> CreateAsync(
        long userId,
        CreatePayoutRequestDTO request);

    // Seller xem các yêu cầu rút tiền của Shop mình
    Task<IEnumerable<PayoutRequestResponseDTO>> GetMyAsync(
        long userId,
        long shopId);

    // Admin xem tất cả yêu cầu rút tiền
    Task<IEnumerable<PayoutRequestResponseDTO>> GetAllAsync();

    // Admin xem chi tiết một yêu cầu
    Task<PayoutRequestResponseDTO?> GetByIdAsync(
        long payoutRequestId);

    // Admin duyệt yêu cầu
    Task<PayoutRequestResponseDTO> ApproveAsync(
        long adminUserId,
        long payoutRequestId);

    // Admin từ chối yêu cầu
    Task<PayoutRequestResponseDTO> RejectAsync(
        long adminUserId,
        long payoutRequestId);
}