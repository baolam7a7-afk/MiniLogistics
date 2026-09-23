using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.Shop;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.BLL.Services.Shop;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shops")]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopController(IShopService shopService)
    {
        _shopService = shopService;
    }

    [HttpPost]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>> Create(
        [FromBody] CreateShopDTO request)
    {
        var userId = GetCurrentUserId();

        var result =
            await _shopService.CreateAsync(
                userId,
                request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpGet("my-shops")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<IEnumerable<ShopResponseDTO>>>
        GetMyShops()
    {
        var userId = GetCurrentUserId();

        var result =
            await _shopService
                .GetMyShopsAsync(userId);

        return Ok(result);
    }

    [HttpGet("my-shops/{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        GetMyShopById(long id)
    {
        var userId = GetCurrentUserId();

        var result =
            await _shopService
                .GetMyShopByIdAsync(
                    userId,
                    id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopResponseDTO>>
        GetById(long id)
    {
        var result =
            await _shopService
                .GetByIdAsync(id);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        Update(
            long id,
            [FromBody] UpdateShopDTO request)
    {
        var userId = GetCurrentUserId();

        var result =
            await _shopService
                .UpdateAsync(
                    userId,
                    id,
                    request);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    private long GetCurrentUserId()
    {
        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);

        if (claim == null ||
            !long.TryParse(
                claim.Value,
                out var userId))
        {
            throw new UnauthorizedException(
                "Không xác định được User.");
        }

        return userId;
    }
}