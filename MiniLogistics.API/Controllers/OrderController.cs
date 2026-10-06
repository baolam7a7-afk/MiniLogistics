using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using MiniLogistics.API.Hubs;
using MiniLogistics.BLL.DTOs.Order;
using MiniLogistics.BLL.Services.Order;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IHubContext<ChatHub> _hub;

    public OrderController(
        IOrderService orderService,
        IHubContext<ChatHub> hub)
    {
        _orderService = orderService;
        _hub = hub;
    }


    // =====================================================
    // CREATE ORDER
    // CUSTOMER
    // =====================================================

    [HttpPost]
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
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] OrderPaginationRequestDTO request)
    {
        long customerId =
            GetCurrentUserId();

        var result =
            await _orderService.GetMyOrdersAsync(
                customerId,
                request);

        return Ok(result);
    }


    // =====================================================
    // GET BY ID
    // CUSTOMER / SELLER / ADMIN
    // =====================================================

    [HttpGet("{id:long}")]
    [Authorize(Roles = "customer,seller,admin,shipper")]
    public async Task<IActionResult> GetById(
        long id)
    {
        long userId =
            GetCurrentUserId();

        string role =
            GetCurrentRole();

        var result =
            await _orderService.GetByIdAsync(
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
    public async Task<IActionResult> GetAll(
        [FromQuery] OrderPaginationRequestDTO request)
    {
        var result =
            await _orderService.GetAllAsync(
                request);

        return Ok(result);
    }


    // =====================================================
    // GET SHOP ORDERS
    // SELLER
    // =====================================================

    [HttpGet("shop")]
    [Authorize(Roles = "seller")]
    public async Task<IActionResult> GetShopOrders(
        [FromQuery] OrderPaginationRequestDTO request)
    {
        long sellerUserId =
            GetCurrentUserId();

        var result =
            await _orderService.GetByShopOwnerAsync(
                sellerUserId,
                request);

        return Ok(result);
    }


    // =====================================================
    // SHOP CONFIRM
    // SELLER / ADMIN
    // =====================================================

    [HttpPost("{id:long}/confirm")]
    [Authorize(Roles = "seller,admin")]
    public async Task<IActionResult> Confirm(
        long id)
    {
        var result =
            await _orderService.ConfirmByShopAsync(
                id,
                GetCurrentUserId(),
                GetCurrentRole());

        return Ok(result);
    }


    // =====================================================
    // SHOP CANCEL
    // SELLER / ADMIN
    // =====================================================

    [HttpPost("{id:long}/shop-cancel")]
    [Authorize(Roles = "seller,admin")]
    public async Task<IActionResult> CancelByShop(
        long id,
        [FromBody] CancelOrderDTO request)
    {
        var result =
            await _orderService.CancelByShopAsync(
                id,
                GetCurrentUserId(),
                GetCurrentRole(),
                request);

        return Ok(result);
    }


    // =====================================================
    // SHIPPER: ORDERS READY TO ACCEPT
    // =====================================================

    [HttpGet("available")]
    [Authorize(Roles = "shipper")]
    public async Task<IActionResult> GetAvailable()
    {
        var result =
            await _orderService.GetAvailableForShipperAsync(
                GetCurrentUserId());

        return Ok(result);
    }


    // =====================================================
    // SHIPPER: ORDERS ASSIGNED TO ME
    // =====================================================

    [HttpGet("delivering")]
    [Authorize(Roles = "shipper")]
    public async Task<IActionResult> GetDelivering()
    {
        var result =
            await _orderService.GetShipperDeliveriesAsync(
                GetCurrentUserId());

        return Ok(result);
    }


    // =====================================================
    // SHIPPER ACCEPTS THE ORDER
    // =====================================================

    [HttpPost("{id:long}/accept")]
    [Authorize(Roles = "shipper")]
    public async Task<IActionResult> Accept(
        long id)
    {
        var shipperUserId = GetCurrentUserId();
        var result =
            await _orderService.AcceptByShipperAsync(
                id,
                shipperUserId);

        await _hub.Clients
            .Group(ChatHub.ShipperGroup)
            .SendAsync("OrderClaimed", new
            {
                orderId = id,
                shipperUserId
            });

        return Ok(result);
    }


    // =====================================================
    // SHIPPER MARKS HANDED TO CUSTOMER
    // =====================================================

    [HttpPost("{id:long}/deliver")]
    [Authorize(Roles = "shipper")]
    public async Task<IActionResult> Deliver(
        long id)
    {
        var result =
            await _orderService.DeliverByShipperAsync(
                id,
                GetCurrentUserId());

        return Ok(result);
    }


    // =====================================================
    // CUSTOMER CONFIRMS RECEIPT
    // =====================================================

    [HttpPost("{id:long}/received")]
    public async Task<IActionResult> Received(
        long id)
    {
        var result =
            await _orderService.ConfirmReceivedAsync(
                id,
                GetCurrentUserId());

        return Ok(result);
    }


    // =====================================================
    // CANCEL
    // CUSTOMER
    // =====================================================

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long id)
    {
        long customerId =
            GetCurrentUserId();

        var result =
            await _orderService.CancelAsync(
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
            await _orderService.UpdateStatusAsync(
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

        if (!long.TryParse(
                value,
                out long userId))
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
        var roles = User.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .ToList();

        if (roles.Count == 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được Role.");
        }

        foreach (var preferred in new[] { "admin", "seller", "shipper", "customer" })
        {
            var match = roles.FirstOrDefault(role =>
                role.Equals(preferred, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }
        }

        return roles[0];
    }
}