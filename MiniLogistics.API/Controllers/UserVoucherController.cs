using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.Services.UserVoucher;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/user-vouchers")]
[Authorize]
public class UserVoucherController : ControllerBase
{
    private readonly IUserVoucherService _service;

    public UserVoucherController(IUserVoucherService service)
    {
        _service = service;
    }

    private long GetUserId() =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine() =>
        Ok(await _service.GetMyVouchersAsync(GetUserId()));

    [HttpGet("claimable")]
    public async Task<IActionResult> GetClaimable([FromQuery] long? shopId = null) =>
        Ok(await _service.GetClaimableAsync(GetUserId(), shopId));

    [HttpPost("{voucherId:long}/claim")]
    public async Task<IActionResult> Claim(long voucherId) =>
        Ok(await _service.ClaimAsync(GetUserId(), voucherId));
}
