using MiniLogistics.BLL.DTOs.OrderVoucher;

namespace MiniLogistics.BLL.Services.OrderVoucher;

public interface IOrderVoucherService
{
    Task<OrderVoucherResponseDTO> ApplyAsync(
        long customerId,
        ApplyVoucherDTO request);

    Task<IEnumerable<OrderVoucherResponseDTO>>
        GetByOrderIdAsync(
            long customerId,
            long orderId);

    Task RemoveAsync(
        long customerId,
        long orderVoucherId);
}