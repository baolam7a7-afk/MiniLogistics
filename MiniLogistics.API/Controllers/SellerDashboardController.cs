using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.Services.SellerDashboard;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/seller/dashboard")]
[Authorize(Roles = "seller")]
public class SellerDashboardController : ControllerBase
{
    private readonly ISellerDashboardService _service;

    public SellerDashboardController(
        ISellerDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userIdClaim = User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            return Unauthorized();
        }

        if (!long.TryParse(
            userIdClaim.Value,
            out var userId))
        {
            return Unauthorized();
        }

        var result =
            await _service.GetDashboardAsync(userId);

        return Ok(result);
    }
}