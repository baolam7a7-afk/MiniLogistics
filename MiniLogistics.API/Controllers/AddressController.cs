using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.Address;
using MiniLogistics.BLL.Services.Address;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;

    public AddressController(
        IAddressService addressService)
    {
        _addressService = addressService;
    }

    private long GetCurrentUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException(
                "User ID not found in token.");
        }

        return long.Parse(userId);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAddresses()
    {
        var userId = GetCurrentUserId();

        var result =
            await _addressService
                .GetMyAddressesAsync(userId);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var userId = GetCurrentUserId();

        var result =
            await _addressService
                .GetByIdAsync(userId, id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAddressDto dto)
    {
        var userId = GetCurrentUserId();

        var result =
            await _addressService
                .CreateAsync(userId, dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateAddressDto dto)
    {
        var userId = GetCurrentUserId();

        var result =
            await _addressService
                .UpdateAsync(userId, id, dto);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var userId = GetCurrentUserId();

        var result =
            await _addressService
                .DeleteAsync(userId, id);

        if (!result)
            return NotFound();

        return NoContent();
    }
}