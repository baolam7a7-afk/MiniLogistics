using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Shipment;
using MiniLogistics.BLL.Services.Shipment;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentController(
        IShipmentService shipmentService)
    {
        _shipmentService =
            shipmentService;
    }

    // =====================================================
    // CREATE SHIPMENT
    // ADMIN / SELLER
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Create(
        [FromBody] CreateShipmentDTO request)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _shipmentService
                .CreateAsync(
                    userId,
                    role,
                    request);

        return Ok(result);
    }

    // =====================================================
    // GET ALL
    // ADMIN
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _shipmentService
                .GetAllAsync();

        return Ok(result);
    }

    // =====================================================
    // GET MY SHIPMENTS
    // SHIPPER
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "shipper")]
    public async Task<IActionResult> GetMy()
    {
        long shipperUserId =
            GetCurrentUserId();

        var result =
            await _shipmentService
                .GetMyShipmentsAsync(
                    shipperUserId);

        return Ok(result);
    }

    // =====================================================
    // GET BY ID
    // =====================================================

    [HttpGet("{id:long}")]
    [Authorize(Roles = "admin,seller,shipper")]
    public async Task<IActionResult> GetById(
        long id)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _shipmentService
                .GetByIdAsync(
                    id,
                    userId,
                    role);

        return Ok(result);
    }

    // =====================================================
    // ASSIGN SHIPPER
    // ADMIN / SELLER
    // =====================================================

    [HttpPut("{id:long}/assign")]
    [Authorize(Roles = "admin,seller")]
    public async Task<IActionResult> Assign(
        long id,
        [FromBody] AssignShipperDTO request)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _shipmentService
                .AssignShipperAsync(
                    id,
                    userId,
                    role,
                    request);

        return Ok(result);
    }

    // =====================================================
    // UPDATE STATUS
    // SHIPPER
    // =====================================================

    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "shipper")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateShipmentStatusDTO request)
    {
        long shipperUserId =
            GetCurrentUserId();

        var result =
            await _shipmentService
                .UpdateStatusAsync(
                    id,
                    shipperUserId,
                    request);

        return Ok(result);
    }

    // =====================================================
    // CURRENT USER
    // =====================================================

    private long GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                value,
                out long userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được User ID.");
        }

        return userId;
    }

    private string GetCurrentRole()
    {
        var role =
            User.FindFirstValue(
                ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được Role.");
        }

        return role;
    }
}