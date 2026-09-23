using MiniLogistics.BLL.DTOs.Address;

namespace MiniLogistics.BLL.Services.Address;

public interface IAddressService
{
    Task<List<AddressResponseDto>> GetMyAddressesAsync(
        long userId);

    Task<AddressResponseDto?> GetByIdAsync(
        long userId,
        long addressId);

    Task<AddressResponseDto> CreateAsync(
        long userId,
        CreateAddressDto dto);

    Task<AddressResponseDto?> UpdateAsync(
        long userId,
        long addressId,
        UpdateAddressDto dto);

    Task<bool> DeleteAsync(
        long userId,
        long addressId);
}