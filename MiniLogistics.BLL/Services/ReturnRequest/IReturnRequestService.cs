using MiniLogistics.BLL.DTOs.ReturnRequest;

namespace MiniLogistics.BLL.Services.ReturnRequest;

public interface IReturnRequestService
{
    Task<ReturnRequestResponseDTO> CreateAsync(
        long customerId,
        CreateReturnRequestDTO request);

    Task<IEnumerable<ReturnRequestResponseDTO>> GetMyRequestsAsync(
        long customerId);

    Task<ReturnRequestResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);

    Task<IEnumerable<ReturnRequestResponseDTO>> GetShopRequestsAsync(
        long sellerId);

    Task<IEnumerable<ReturnRequestResponseDTO>> GetAllAsync();

    Task<ReturnRequestResponseDTO> ApproveAsync(
        long handlerUserId,
        string role,
        long id);

    Task<ReturnRequestResponseDTO> RejectAsync(
        long handlerUserId,
        string role,
        long id);
}