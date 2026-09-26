using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.SupportMessage;

namespace MiniLogistics.BLL.Services.SupportMessage;

public interface ISupportMessageService
{
    // =====================================================
    // CREATE
    // =====================================================

    Task<SupportMessageResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateSupportMessageDTO request);


    // =====================================================
    // GET BY ID
    // =====================================================

    Task<SupportMessageResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);


    // =====================================================
    // GET BY TICKET ID - PAGINATION
    // =====================================================

    Task<PagedResponseDTO<SupportMessageResponseDTO>>
        GetByTicketIdAsync(
            long userId,
            string role,
            long ticketId,
            SupportMessagePaginationRequestDTO request);


    // =====================================================
    // UPDATE
    // =====================================================

    Task<SupportMessageResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateSupportMessageDTO request);


    // =====================================================
    // DELETE
    // =====================================================

    Task DeleteAsync(
        long userId,
        string role,
        long id);
}