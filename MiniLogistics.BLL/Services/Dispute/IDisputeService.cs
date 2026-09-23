using MiniLogistics.BLL.DTOs.Dispute;

namespace MiniLogistics.BLL.Services.Dispute;

public interface IDisputeService
{
    // =========================================================
    // CREATE
    // =========================================================

    Task<DisputeResponseDTO> CreateAsync(
        long userId,
        CreateDisputeDTO request);


    // =========================================================
    // GET BY ID
    // =========================================================

    Task<DisputeResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);


    // =========================================================
    // GET MY DISPUTES
    // =========================================================

    Task<IEnumerable<DisputeResponseDTO>>
        GetMyDisputesAsync(
            long userId);


    // =========================================================
    // GET ALL
    // =========================================================

    Task<IEnumerable<DisputeResponseDTO>>
        GetAllAsync();


    // =========================================================
    // UPDATE
    // =========================================================

    Task<DisputeResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateDisputeDTO request);


    // =========================================================
    // DELETE
    // =========================================================

    Task DeleteAsync(
        long userId,
        string role,
        long id);
}