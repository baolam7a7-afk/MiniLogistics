using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.DisputeMessage;

namespace MiniLogistics.BLL.Services.DisputeMessage;

public interface IDisputeMessageService
{
    // =====================================================
    // CREATE
    // =====================================================

    Task<DisputeMessageResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateDisputeMessageDTO request);


    // =====================================================
    // GET BY ID
    // =====================================================

    Task<DisputeMessageResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);


    // =====================================================
    // GET BY DISPUTE - PAGINATION
    // =====================================================

    Task<PagedResponseDTO<DisputeMessageResponseDTO>>
        GetByDisputeIdAsync(
            long userId,
            string role,
            long disputeId,
            DisputeMessagePaginationRequestDTO request);


    // =====================================================
    // UPDATE
    // =====================================================

    Task<DisputeMessageResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateDisputeMessageDTO request);


    // =====================================================
    // DELETE
    // =====================================================

    Task DeleteAsync(
        long userId,
        string role,
        long id);
}