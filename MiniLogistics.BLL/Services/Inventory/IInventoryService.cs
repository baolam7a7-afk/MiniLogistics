using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Inventory;

namespace MiniLogistics.BLL.Services.Inventory;

public interface IInventoryService
{
    // =====================================================
    // GET ALL - PAGINATION
    // =====================================================

    Task<PagedResponseDTO<InventoryResponseDTO>>
        GetAllAsync(
            InventoryPaginationRequestDTO request);


    // =====================================================
    // GET BY VARIANT
    // =====================================================

    Task<InventoryResponseDTO>
        GetByVariantIdAsync(
            long productVariantId);


    // =====================================================
    // INCREASE
    // =====================================================

    Task<InventoryResponseDTO>
        IncreaseAsync(
            long productVariantId,
            IncreaseInventoryDTO request);


    // =====================================================
    // DECREASE
    // =====================================================

    Task<InventoryResponseDTO>
        DecreaseAsync(
            long productVariantId,
            DecreaseInventoryDTO request);


    // =====================================================
    // ADJUST
    // =====================================================

    Task<InventoryResponseDTO>
        AdjustAsync(
            long productVariantId,
            AdjustInventoryDTO request);


    // =====================================================
    // RESERVE
    // =====================================================

    Task ReserveAsync(
        long productVariantId,
        int quantity);


    // =====================================================
    // RELEASE
    // =====================================================

    Task ReleaseAsync(
        long productVariantId,
        int quantity);


    // =====================================================
    // DEDUCT
    // =====================================================

    Task DeductAsync(
        long productVariantId,
        int quantity);
}