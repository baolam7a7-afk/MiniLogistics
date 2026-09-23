using MiniLogistics.BLL.DTOs.Voucher;

namespace MiniLogistics.BLL.Services.Voucher;

public interface IVoucherService
{
    Task<IEnumerable<VoucherResponseDTO>> GetAllAsync();

    Task<VoucherResponseDTO?> GetByIdAsync(long id);

    Task<IEnumerable<VoucherResponseDTO>> GetByShopIdAsync(
        long shopId);

    Task<VoucherResponseDTO?> GetByCodeAsync(
        string code);

    Task<VoucherResponseDTO> CreateAsync(
        long actorUserId,
        string actorRole,
        CreateVoucherDTO request);

    Task<VoucherResponseDTO> UpdateAsync(
        long id,
        long actorUserId,
        string actorRole,
        UpdateVoucherDTO request);

    Task DeleteAsync(
        long id,
        long actorUserId,
        string actorRole);

    Task<VoucherResponseDTO> ValidateAsync(
        string code,
        decimal orderValue,
        long? shopId);
}