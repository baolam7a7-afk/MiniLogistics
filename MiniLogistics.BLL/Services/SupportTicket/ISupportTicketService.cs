using MiniLogistics.BLL.DTOs.SupportTicket;

namespace MiniLogistics.BLL.Services.SupportTicket;

public interface ISupportTicketService
{
    // =========================================================
    // CREATE
    // =========================================================

    Task<SupportTicketResponseDTO> CreateAsync(
        long userId,
        CreateSupportTicketDTO request);


    // =========================================================
    // GET BY ID
    // =========================================================

    Task<SupportTicketResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);


    // =========================================================
    // GET MY TICKETS
    // =========================================================

    Task<IEnumerable<SupportTicketResponseDTO>>
        GetMyTicketsAsync(
            long userId);


    // =========================================================
    // GET ALL
    // =========================================================

    Task<IEnumerable<SupportTicketResponseDTO>>
        GetAllAsync();


    // =========================================================
    // UPDATE
    // =========================================================

    Task<SupportTicketResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateSupportTicketDTO request);


    // =========================================================
    // DELETE
    // =========================================================

    Task DeleteAsync(
        long userId,
        string role,
        long id);
}