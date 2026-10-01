using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.Services.QrPayment;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/payments/qr")]
[Authorize]
public class QrPaymentController : ControllerBase
{
    private readonly IQrPaymentService _service;

    public QrPaymentController(IQrPaymentService service)
    {
        _service = service;
    }

    private long GetUserId() =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("orders/{orderId:long}")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Create(long orderId) =>
        Ok(await _service.CreateOrGetAsync(GetUserId(), orderId));

    [HttpGet("{paymentId:long}")]
    public async Task<IActionResult> GetStatus(long paymentId) =>
        Ok(await _service.GetStatusAsync(GetUserId(), paymentId));

    /// <summary>
    /// Demo/manual confirm — dùng khi chưa có webhook ngân hàng thật.
    /// </summary>
    [HttpPost("{paymentId:long}/confirm")]
    public async Task<IActionResult> Confirm(long paymentId)
    {
        var isAdmin = User.IsInRole("admin");
        return Ok(await _service.ConfirmPaidAsync(GetUserId(), paymentId, isAdmin));
    }
}
