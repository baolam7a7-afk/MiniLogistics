using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.Services.AdminDashboard;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _service;

    public AdminDashboardController(
        IAdminDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var result =
            await _service.GetDashboardAsync();

        return Ok(result);
    }
}