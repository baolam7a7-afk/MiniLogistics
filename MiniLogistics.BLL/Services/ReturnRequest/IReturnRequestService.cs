using MiniLogistics.BLL.DTOs.ReturnRequest;
using MiniLogistics.BLL.DTOs.Common;

namespace MiniLogistics.BLL.Services.ReturnRequest;

public interface IReturnRequestService
{
    // =====================================================
    // CREATE
    // =====================================================

    Task<ReturnRequestResponseDTO> CreateAsync(
        long customerId,
        CreateReturnRequestDTO request);

    // =====================================================
    // CUSTOMER
    // MY REQUESTS
    // =====================================================

    Task<PagedResponseDTO<ReturnRequestResponseDTO>> GetMyRequestsAsync(
        long customerId,
        ReturnRequestPaginationRequestDTO request);

    // =====================================================
    // GET BY ID
    // =====================================================

    Task<ReturnRequestResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);

    // =====================================================
    // SELLER
    // SHOP REQUESTS
    // =====================================================

    Task<PagedResponseDTO<ReturnRequestResponseDTO>> GetShopRequestsAsync(
        long sellerId,
        ReturnRequestPaginationRequestDTO request);

    // =====================================================
    // ADMIN
    // ALL
    // =====================================================

    Task<PagedResponseDTO<ReturnRequestResponseDTO>> GetAllAsync(
        ReturnRequestPaginationRequestDTO request);

    // =====================================================
    // APPROVE
    // =====================================================

    Task<ReturnRequestResponseDTO> ApproveAsync(
        long handlerUserId,
        string role,
        long id);

    // =====================================================
    // REJECT
    // =====================================================

    Task<ReturnRequestResponseDTO> RejectAsync(
        long handlerUserId,
        string role,
        long id);
}