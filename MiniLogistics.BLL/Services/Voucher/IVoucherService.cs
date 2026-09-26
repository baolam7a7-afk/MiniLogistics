using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Voucher;

namespace MiniLogistics.BLL.Services.Voucher;

public interface IVoucherService
{
    // =====================================================
    // GET ALL
    // =====================================================

    Task<PagedResponseDTO<VoucherResponseDTO>> GetAllAsync(
        VoucherPaginationRequestDTO request);


    // =====================================================
    // GET BY ID
    // =====================================================

    Task<VoucherResponseDTO?> GetByIdAsync(
        long id);


    // =====================================================
    // GET BY SHOP
    // =====================================================

    Task<PagedResponseDTO<VoucherResponseDTO>> GetByShopIdAsync(
        long shopId,
        VoucherPaginationRequestDTO request);


    // =====================================================
    // GET BY CODE
    // =====================================================

    Task<VoucherResponseDTO?> GetByCodeAsync(
        string code);


    // =====================================================
    // CREATE
    // =====================================================

    Task<VoucherResponseDTO> CreateAsync(
        long actorUserId,
        string actorRole,
        CreateVoucherDTO request);


    // =====================================================
    // UPDATE
    // =====================================================

    Task<VoucherResponseDTO> UpdateAsync(
        long id,
        long actorUserId,
        string actorRole,
        UpdateVoucherDTO request);


    // =====================================================
    // DELETE
    // =====================================================

    Task DeleteAsync(
        long id,
        long actorUserId,
        string actorRole);


    // =====================================================
    // VALIDATE
    // =====================================================

    Task<VoucherResponseDTO> ValidateAsync(
        string code,
        decimal orderValue,
        long? shopId);
}