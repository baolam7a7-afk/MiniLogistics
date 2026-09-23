using MiniLogistics.BLL.DTOs.RefundTransaction;

namespace MiniLogistics.BLL.Services.RefundTransaction;

public interface IRefundTransactionService
{
    Task<RefundTransactionResponseDTO> CreateAsync(
        long adminUserId,
        CreateRefundTransactionDTO request);

    Task<IEnumerable<RefundTransactionResponseDTO>>
        GetMyRefundsAsync(long customerId);

    Task<IEnumerable<RefundTransactionResponseDTO>>
        GetAllAsync();

    Task<RefundTransactionResponseDTO?>
        GetByIdAsync(long userId, string role, long refundId);

    Task<RefundTransactionResponseDTO>
        CompleteAsync(
            long adminUserId,
            long refundId);

    Task<RefundTransactionResponseDTO>
        FailAsync(
            long adminUserId,
            long refundId);
}