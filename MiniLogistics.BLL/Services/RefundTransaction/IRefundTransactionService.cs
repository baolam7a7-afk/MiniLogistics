using MiniLogistics.BLL.DTOs.RefundTransaction;
using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.Services.RefundTransaction;

public interface IRefundTransactionService
{
    // =====================================================
    // CREATE
    // =====================================================

    Task<RefundTransactionResponseDTO> CreateAsync(
        long adminUserId,
        CreateRefundTransactionDTO request);

    // =====================================================
    // CUSTOMER
    // =====================================================

    Task<PagedResponseDTO<RefundTransactionResponseDTO>>
        GetMyRefundsAsync(
            long customerId,
            RefundPaginationRequestDTO request);

    // =====================================================
    // ADMIN
    // =====================================================

    Task<PagedResponseDTO<RefundTransactionResponseDTO>>
        GetAllAsync(
            RefundPaginationRequestDTO request);

    // =====================================================
    // GET BY ID
    // =====================================================

    Task<RefundTransactionResponseDTO?>
        GetByIdAsync(
            long userId,
            string role,
            long refundId);

    // =====================================================
    // COMPLETE
    // =====================================================

    Task<RefundTransactionResponseDTO>
        CompleteAsync(
            long adminUserId,
            long refundId);

    // =====================================================
    // FAIL
    // =====================================================

    Task<RefundTransactionResponseDTO>
        FailAsync(
            long adminUserId,
            long refundId);
}