using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Payment;

namespace MiniLogistics.BLL.Services.Payment;

public interface IPaymentService
{
    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    Task<PagedResponseDTO<PaymentResponseDTO>> GetAllAsync(
        PaymentPaginationRequestDTO request);


    // =====================================================
    // GET MY PAYMENTS
    // CUSTOMER
    // =====================================================

    Task<PagedResponseDTO<PaymentResponseDTO>> GetMyPaymentsAsync(
        long customerId,
        PaymentPaginationRequestDTO request);


    // =====================================================
    // GET BY ID
    // CUSTOMER / SELLER / ADMIN
    // =====================================================

    Task<PaymentResponseDTO> GetByIdAsync(
        long paymentId,
        long userId,
        string role);


    // =====================================================
    // GET BY ORDER
    // CUSTOMER / SELLER / ADMIN
    // =====================================================

    Task<PaymentResponseDTO> GetByOrderIdAsync(
        long orderId,
        long userId,
        string role);


    // =====================================================
    // UPDATE STATUS
    // ADMIN
    // =====================================================

    Task<PaymentResponseDTO> UpdateStatusAsync(
        long paymentId,
        UpdatePaymentStatusDTO request);
}