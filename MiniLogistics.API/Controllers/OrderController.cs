using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MiniLogistics.BLL.DTOs.Order;
using MiniLogistics.BLL.Services.Order;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;


    public OrderController(
        IOrderService orderService)
    {
        _orderService = orderService;
    }


    // =====================================================
    // CREATE ORDER
    // CUSTOMER
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderDTO request)
    {
        long customerId =
            GetCurrentUserId();

        var result =
            await _orderService.CreateAsync(
                customerId,
                request);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Id
            },
            result);
    }


    // =====================================================
    // GET MY ORDERS
    // CUSTOMER
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> GetMyOrders()
    {
        long customerId =
            GetCurrentUserId();

        var result =
            await _orderService
                .GetMyOrdersAsync(customerId);

        return Ok(result);
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    [HttpGet("{id:long}")]
    [Authorize(Roles = "customer,seller,admin")]
    public async Task<IActionResult> GetById(
        long id)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _orderService
                .GetByIdAsync(
                    id,
                    userId,
                    role);

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
            await _orderService
                .GetAllAsync();

        return Ok(result);
    }


    // =====================================================
    // GET SHOP ORDERS
    // SELLER
    // =====================================================

    [HttpGet("shop")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> GetShopOrders()
    {
        long sellerUserId =
            GetCurrentUserId();

        var result =
            await _orderService
                .GetByShopOwnerAsync(
                    sellerUserId);

        return Ok(result);
    }


    // =====================================================
    // CANCEL
    // CUSTOMER
    // =====================================================

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = "customer")]
    public async Task<IActionResult> Cancel(
        long id)
    {
        long customerId =
            GetCurrentUserId();

        var result =
            await _orderService
                .CancelAsync(
                    id,
                    customerId);

        return Ok(result);
    }


    // =====================================================
    // UPDATE STATUS
    // SELLER / ADMIN
    // =====================================================

    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "seller,admin")]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateOrderStatusDTO request)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _orderService
                .UpdateStatusAsync(
                    id,
                    userId,
                    role,
                    request);

        return Ok(result);
    }


    // =====================================================
    // CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(value, out long userId))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được User ID.");
        }

        return userId;
    }


    // =====================================================
    // CURRENT ROLE
    // =====================================================

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