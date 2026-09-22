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
        _inventoryService =
            inventoryService;
    }

    // =====================================================
    // GET ALL
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _inventoryService.GetAllAsync();

        return Ok(result);
    }

    // =====================================================
    // GET BY VARIANT
    // =====================================================

    [HttpGet("variant/{productVariantId:long}")]
    public async Task<IActionResult>
        GetByVariantId(
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
    public async Task<IActionResult>
        Increase(
            long productVariantId,
            [FromBody]
            IncreaseInventoryDTO request)
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
    public async Task<IActionResult>
        Decrease(
            long productVariantId,
            [FromBody]
            DecreaseInventoryDTO request)
    {
        var result =
            await _inventoryService
                .DecreaseAsync(
                    productVariantId,
                    request);

        return Ok(result);
    }

    // =====================================================
    // ADJUST asdasd
    // =====================================================

    [HttpPut(
        "variant/{productVariantId:long}/adjust")]
    public async Task<IActionResult>
        Adjust(
            long productVariantId,
            [FromBody]
            AdjustInventoryDTO request)
    {
        var result =
            await _inventoryService
                .AdjustAsync(
                    productVariantId,
                    request);

        return Ok(result);
    }
}