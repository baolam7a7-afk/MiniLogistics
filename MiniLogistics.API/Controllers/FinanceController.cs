using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.BLL.Services.Finance;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/finance")]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _finance;

    public FinanceController(IFinanceService finance)
    {
        _finance = finance;
    }

    [HttpGet("platform")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Platform([FromQuery] long? shopId) =>
        Ok(await _finance.GetPlatformAsync(shopId));

    [HttpGet("seller")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> Seller([FromQuery] long shopId)
    {
        if (shopId <= 0)
        {
            return BadRequest(new { message = "Chọn cửa hàng." });
        }

        return Ok(await _finance.GetSellerShopAsync(GetUserId(), shopId));
    }

    private long GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(value, out var userId))
        {
            throw new UnauthorizedException("Token không chứa UserId hợp lệ.");
        }

        return userId;
    }
}
