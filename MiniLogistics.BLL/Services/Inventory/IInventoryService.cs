using MiniLogistics.BLL.DTOs.Inventory;

namespace MiniLogistics.BLL.Services.Inventory;

public interface IInventoryService
{
    Task<IEnumerable<InventoryResponseDTO>> GetAllAsync();

    Task<InventoryResponseDTO> GetByVariantIdAsync(
        long productVariantId);

    Task<InventoryResponseDTO> IncreaseAsync(
        long productVariantId,
        IncreaseInventoryDTO request);

    Task<InventoryResponseDTO> DecreaseAsync(
        long productVariantId,
        DecreaseInventoryDTO request);

    Task<InventoryResponseDTO> AdjustAsync(
        long productVariantId,
        AdjustInventoryDTO request);

    // Dùng sau này khi Order tạo
    Task ReserveAsync(
        long productVariantId,
        int quantity);

    // Dùng khi Order bị hủy / Payment thất bại
    Task ReleaseAsync(
        long productVariantId,
        int quantity);

    // Dùng khi Payment thành công
    Task DeductAsync(
        long productVariantId,
        int quantity);
}