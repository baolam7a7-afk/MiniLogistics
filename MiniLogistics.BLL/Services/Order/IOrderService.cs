using MiniLogistics.BLL.DTOs.Order;

namespace MiniLogistics.BLL.Services.Order;

public interface IOrderService
{
    Task<OrderResponseDTO> CreateAsync(
        long customerId,
        CreateOrderDTO request);


    Task<IEnumerable<OrderResponseDTO>> GetMyOrdersAsync(
        long customerId);


    Task<OrderResponseDTO> GetByIdAsync(
        long orderId,
        long userId,
        string role);


    Task<IEnumerable<OrderResponseDTO>> GetAllAsync();


    Task<IEnumerable<OrderResponseDTO>> GetByShopOwnerAsync(
        long sellerUserId);


    Task<OrderResponseDTO> CancelAsync(
        long orderId,
        long customerId);


    Task<OrderResponseDTO> UpdateStatusAsync(
        long orderId,
        long actorUserId,
        string role,
        UpdateOrderStatusDTO request);
}