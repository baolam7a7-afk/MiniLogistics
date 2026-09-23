using MiniLogistics.BLL.DTOs.ReportSnapshot;

namespace MiniLogistics.BLL.Services.ReportSnapshot;

public interface IReportSnapshotService
{
    // =========================================================
    // CREATE
    // =========================================================

    Task<ReportSnapshotResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateReportSnapshotDTO request);


    // =========================================================
    // GET BY ID
    // =========================================================

    Task<ReportSnapshotResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id);


    // =========================================================
    // GET ALL
    // ADMIN ONLY
    // =========================================================

    Task<IEnumerable<ReportSnapshotResponseDTO>>
        GetAllAsync();


    // =========================================================
    // GET BY SHOP
    // =========================================================

    Task<IEnumerable<ReportSnapshotResponseDTO>>
        GetByShopIdAsync(
            long userId,
            string role,
            long shopId);


    // =========================================================
    // UPDATE
    // =========================================================

    Task<ReportSnapshotResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateReportSnapshotDTO request);


    // =========================================================
    // DELETE
    // =========================================================

    Task DeleteAsync(
        long userId,
        string role,
        long id);
}