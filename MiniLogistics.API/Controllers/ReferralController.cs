using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.Services.Referral;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/referrals")]
[Authorize]
public class ReferralController : ControllerBase
{
    private readonly IReferralService _referral;

    public ReferralController(IReferralService referral)
    {
        _referral = referral;
    }

    private long UserId() =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("my-code")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> MyCode() =>
        Ok(await _referral.GetOrCreateMyCodeAsync(UserId()));

    [HttpGet("policy")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Policy() =>
        Ok(await _referral.GetPolicyAsync());

    [HttpPut("policy")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateReferralPolicyDTO request) =>
        Ok(await _referral.UpdatePolicyAsync(request));

    [HttpGet("summary")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Summary() =>
        Ok(await _referral.GetSummaryAsync());

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> List([FromQuery] string? status) =>
        Ok(await _referral.ListAsync(status));

    [HttpPost("{id:long}/evaluate")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Evaluate(long id) =>
        Ok(await _referral.EvaluateAsync(id, UserId()));

    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Approve(long id) =>
        Ok(await _referral.ApprovePayoutAsync(id, UserId()));

    [HttpPost("{id:long}/reject")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Reject(long id, [FromBody] ReferralNoteRequest request) =>
        Ok(await _referral.RejectAsync(id, UserId(), request.Reason));

    [HttpPost("{id:long}/flag")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Flag(long id, [FromBody] ReferralNoteRequest request) =>
        Ok(await _referral.FlagAsync(id, UserId(), request.Reason));

    [HttpPost("{id:long}/unflag")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Unflag(long id) =>
        Ok(await _referral.UnflagAsync(id, UserId()));

    [HttpPost("settle")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Settle() =>
        Ok(new { paid = await _referral.SettleDueAsync(UserId()) });
}

public class ReferralNoteRequest
{
    public string Reason { get; set; } = "";
}
