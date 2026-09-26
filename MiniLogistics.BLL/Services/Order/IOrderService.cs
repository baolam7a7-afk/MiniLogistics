using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Order;

namespace MiniLogistics.BLL.Services.Order;

public interface IOrderService
{
    Task<OrderResponseDTO> CreateAsync(
        long customerId,
        CreateOrderDTO request);

    Task<PagedResponseDTO<OrderResponseDTO>> GetMyOrdersAsync(
        long customerId,
        OrderPaginationRequestDTO request);

    Task<OrderResponseDTO> GetByIdAsync(
        long orderId,
        long userId,
        string role);

    Task<PagedResponseDTO<OrderResponseDTO>> GetAllAsync(
        OrderPaginationRequestDTO request);

    Task<PagedResponseDTO<OrderResponseDTO>> GetByShopOwnerAsync(
        long sellerUserId,
        OrderPaginationRequestDTO request);

    Task<OrderResponseDTO> CancelAsync(
        long orderId,
        long customerId);

    Task<OrderResponseDTO> UpdateStatusAsync(
        long orderId,
        long actorUserId,
        string role,
        UpdateOrderStatusDTO request);
}