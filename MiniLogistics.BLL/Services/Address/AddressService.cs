using Microsoft.EntityFrameworkCore;
using MiniLogistics.BLL.DTOs.Address;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Data;
using AddressModel = MiniLogistics.DAL.Models.Address;

namespace MiniLogistics.BLL.Services.Address;

public class AddressService : IAddressService
{
    private readonly AppDbContext _context;

    public AddressService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AddressResponseDto>> GetMyAddressesAsync(
        long userId)
    {
        return await _context.Addresses
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => MapToDto(x))
            .ToListAsync();
    }

    public async Task<AddressResponseDto?> GetByIdAsync(
        long userId,
        long addressId)
    {
        return await _context.Addresses
            .Where(x =>
                x.Id == addressId &&
                x.UserId == userId)
            .Select(x => MapToDto(x))
            .FirstOrDefaultAsync();
    }

    public async Task<AddressResponseDto> CreateAsync(
        long userId,
        CreateAddressDto dto)
    {
        if (dto.IsDefault)
        {
            var oldDefaults = await _context.Addresses
                .Where(x =>
                    x.UserId == userId &&
                    x.IsDefault)
                .ToListAsync();

            foreach (var address in oldDefaults)
            {
                address.IsDefault = false;
            }
        }

        var addressEntity = new AddressModel
        {
            UserId = userId,

            ReceiverName = dto.ReceiverName,
            ReceiverPhone = dto.ReceiverPhone,

            Line1 = dto.Line1,
            Line2 = dto.Line2,

            Ward = dto.Ward,
            District = dto.District,
            Province = dto.Province,

            Country = dto.Country,
            PostalCode = dto.PostalCode,

            IsDefault = dto.IsDefault,

            CreatedAt = DateTime.UtcNow
        };

        _context.Addresses.Add(addressEntity);

        await _context.SaveChangesAsync();

        return MapToDto(addressEntity);
    }

    public async Task<AddressResponseDto?> UpdateAsync(
        long userId,
        long addressId,
        UpdateAddressDto dto)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(x =>
                x.Id == addressId &&
                x.UserId == userId);

        if (address == null)
            return null;

        if (dto.IsDefault)
        {
            var oldDefaults = await _context.Addresses
                .Where(x =>
                    x.UserId == userId &&
                    x.Id != addressId &&
                    x.IsDefault)
                .ToListAsync();

            foreach (var oldAddress in oldDefaults)
            {
                oldAddress.IsDefault = false;
            }
        }

        address.ReceiverName = dto.ReceiverName;
        address.ReceiverPhone = dto.ReceiverPhone;

        address.Line1 = dto.Line1;
        address.Line2 = dto.Line2;

        address.Ward = dto.Ward;
        address.District = dto.District;
        address.Province = dto.Province;

        address.Country = dto.Country;
        address.PostalCode = dto.PostalCode;

        address.IsDefault = dto.IsDefault;

        address.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(address);
    }

    public async Task<bool> DeleteAsync(
        long userId,
        long addressId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(x =>
                x.Id == addressId &&
                x.UserId == userId);

        if (address == null)
        {
            throw new NotFoundException(
                "Không tìm thấy địa chỉ.");
        }

        var isUsedByOrder = await _context.Orders
            .AnyAsync(x => x.ShippingAddressId == addressId);

        if (isUsedByOrder)
        {
            throw new BadRequestException(
                "Không thể xóa địa chỉ vì địa chỉ đang được sử dụng trong đơn hàng.");
        }

        _context.Addresses.Remove(address);

        await _context.SaveChangesAsync();

        return true;
    }

    private static AddressResponseDto MapToDto(
        AddressModel address)
    {
        return new AddressResponseDto
        {
            Id = address.Id,
            UserId = address.UserId,

            ReceiverName = address.ReceiverName,
            ReceiverPhone = address.ReceiverPhone,

            Line1 = address.Line1,
            Line2 = address.Line2,

            Ward = address.Ward,
            District = address.District,
            Province = address.Province,

            Country = address.Country,
            PostalCode = address.PostalCode,

            IsDefault = address.IsDefault,

            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }
}