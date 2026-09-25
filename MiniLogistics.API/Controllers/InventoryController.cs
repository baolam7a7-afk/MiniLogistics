using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Inventory;
using MiniLogistics.BLL.Services.Inventory;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "admin,seller")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(
        IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }


    // =====================================================
    // GET ALL
    // PAGINATION
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] InventoryPaginationRequestDTO request)
    {
        var result =
            await _inventoryService.GetAllAsync(request);

        return Ok(result);
    }


    // =====================================================
    // GET BY VARIANT
    // =====================================================

    [HttpGet("variant/{productVariantId:long}")]
    public async Task<IActionResult> GetByVariantId(
        long productVariantId)
    {
        var result =
            await _inventoryService
                .GetByVariantIdAsync(
                    productVariantId);

        return Ok(result);
    }


    // =====================================================
    // INCREASE
    // =====================================================

    [HttpPost(
        "variant/{productVariantId:long}/increase")]
    public async Task<IActionResult> Increase(
        long productVariantId,
        [FromBody] IncreaseInventoryDTO request)
    {
        var result =
            await _inventoryService
                .IncreaseAsync(
                    productVariantId,
                    request);

        return Ok(result);
    }


    // =====================================================
    // DECREASE
    // =====================================================

    [HttpPost(
        "variant/{productVariantId:long}/decrease")]
    public async Task<IActionResult> Decrease(
        long productVariantId,
        [FromBody] DecreaseInventoryDTO request)
    {
        var result =
            await _inventoryService
                .DecreaseAsync(
                    productVariantId,
                    request);

        return Ok(result);
    }


    // =====================================================
    // ADJUST
    // =====================================================

    [HttpPut(
        "variant/{productVariantId:long}/adjust")]
    public async Task<IActionResult> Adjust(
        long productVariantId,
        [FromBody] AdjustInventoryDTO request)
    {
        var result =
            await _inventoryService
                .AdjustAsync(
                    productVariantId,
                    request);

        return Ok(result);
    }


    // =====================================================
    // RESERVE
    // =====================================================

    [HttpPost(
        "variant/{productVariantId:long}/reserve")]
    public async Task<IActionResult> Reserve(
        long productVariantId,
        [FromBody] InventoryQuantityDTO request)
    {
        await _inventoryService.ReserveAsync(
            productVariantId,
            request.Quantity);

        var result =
            await _inventoryService
                .GetByVariantIdAsync(
                    productVariantId);

        return Ok(result);
    }


    // =====================================================
    // RELEASE
    // =====================================================

    [HttpPost(
        "variant/{productVariantId:long}/release")]
    public async Task<IActionResult> Release(
        long productVariantId,
        [FromBody] InventoryQuantityDTO request)
    {
        await _inventoryService.ReleaseAsync(
            productVariantId,
            request.Quantity);

        var result =
            await _inventoryService
                .GetByVariantIdAsync(
                    productVariantId);

        return Ok(result);
    }
}