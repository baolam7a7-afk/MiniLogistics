using MiniLogistics.BLL.DTOs.Payment;

namespace MiniLogistics.BLL.Services.Payment;

public interface IPaymentService
{
    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    Task<IEnumerable<PaymentResponseDTO>> GetAllAsync();


    // =====================================================
    // GET MY PAYMENTS
    // CUSTOMER
    // =====================================================

    Task<IEnumerable<PaymentResponseDTO>> GetMyPaymentsAsync(
        long customerId);


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