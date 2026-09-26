using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.SupportTicket;

namespace MiniLogistics.BLL.Services.SupportTicket;

public interface ISupportTicketService
{
    // =====================================================
    // CREATE
    // =====================================================

    Task<SupportTicketResponseDTO> CreateAsync(
        long userId,
        CreateSupportTicketDTO request);


    // =====================================================
    // GET BY ID
    // =====================================================

    Task<SupportTicketResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);


    // =====================================================
    // GET MY TICKETS - PAGINATION
    // =====================================================

    Task<PagedResponseDTO<SupportTicketResponseDTO>>
        GetMyTicketsAsync(
            long userId,
            SupportTicketPaginationRequestDTO request);


    // =====================================================
    // GET ALL - ADMIN - PAGINATION
    // =====================================================

    Task<PagedResponseDTO<SupportTicketResponseDTO>>
        GetAllAsync(
            SupportTicketPaginationRequestDTO request);


    // =====================================================
    // UPDATE
    // =====================================================

    Task<SupportTicketResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateSupportTicketDTO request);


    // =====================================================
    // DELETE
    // =====================================================

    Task DeleteAsync(
        long userId,
        string role,
        long id);
}