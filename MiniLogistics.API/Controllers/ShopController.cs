using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Shop;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.BLL.Services.Chat;
using MiniLogistics.BLL.Services.Shop;
using MiniLogistics.API.Hubs;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/shops")]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;
    private readonly IChatService _chatService;
    private readonly ChatRealtime _chatRealtime;
    private readonly IWebHostEnvironment _environment;
    private const long MaxLogoBytes = 5 * 1024 * 1024;

    public ShopController(
        IShopService shopService,
        IChatService chatService,
        ChatRealtime chatRealtime,
        IWebHostEnvironment environment)
    {
        _shopService = shopService;
        _chatService = chatService;
        _chatRealtime = chatRealtime;
        _environment = environment;
    }


    // =====================================================
    // ADMIN - GET ALL SHOPS
    // =====================================================

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PagedResponseDTO<ShopResponseDTO>>>
        GetAll(
            [FromQuery] ShopPaginationRequestDTO request)
    {
        var result =
            await _shopService.GetAllAsync(request);

        return Ok(result);
    }


    // =====================================================
    // ADMIN - GET PENDING SHOPS
    // =====================================================

    [HttpGet("pending")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<ShopResponseDTO>>>
        GetPending()
    {
        var result =
            await _shopService.GetPendingAsync();

        return Ok(result);
    }


    // =====================================================
    // ADMIN - APPROVE SHOP
    // =====================================================

    [HttpPut("{id:long}/approve")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopResponseDTO>>
        Approve(long id)
    {
        var result =
            await _shopService.ApproveAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Shop."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // ADMIN - REJECT SHOP
    // =====================================================

    [HttpPut("{id:long}/reject")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopResponseDTO>>
        Reject(long id)
    {
        var result =
            await _shopService.RejectAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Shop."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // GET SHOP BY ID
    // PUBLIC
    // =====================================================

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShopResponseDTO>>
        GetById(long id)
    {
        var result =
            await _shopService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Không tìm thấy Shop."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // SELLER - CREATE SHOP
    // =====================================================

    [HttpPost]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        Create(
            [FromBody] CreateShopDTO request)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService.CreateAsync(
                userId,
                request,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = result.Id
            },
            result);
    }


    // =====================================================
    // SELLER - GET MY SHOPS
    // =====================================================

    [HttpGet("my")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<IEnumerable<ShopResponseDTO>>>
        GetMyShops()
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService.GetMyShopsAsync(
                userId);

        return Ok(result);
    }


    // =====================================================
    // SELLER - GET MY SHOP BY ID
    // =====================================================

    [HttpGet("my/{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        GetMyShopById(long id)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService.GetMyShopByIdAsync(
                userId,
                id);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy Shop hoặc Shop không thuộc quyền sở hữu của bạn."
            });
        }

        return Ok(result);
    }


    // =====================================================
    // SELLER - UPDATE MY SHOP
    // =====================================================

    [HttpPut("{id:long}")]
    [Authorize(Roles = "seller")]
    public async Task<ActionResult<ShopResponseDTO>>
        Update(
            long id,
            [FromBody] UpdateShopDTO request)
    {
        var userId =
            GetCurrentUserId();

        var result =
            await _shopService.UpdateAsync(
                userId,
                id,
                request);

        if (result == null)
        {
            return NotFound(new
            {
                message =
                    "Không tìm thấy Shop hoặc Shop không thuộc quyền sở hữu của bạn."
            });
        }

        return Ok(result);
    }

    [HttpPut("{id:long}/selling")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ShopResponseDTO>> SetSelling(
        long id,
        [FromBody] ShopSellingRequest request)
    {
        var result = await _shopService.SetSellingAsync(id, request.Selling, request.Reason);
        if (result == null)
        {
            return NotFound(new { message = "Không tìm thấy Shop." });
        }

        if (!request.Selling)
        {
            var notice = await _chatService.NotifyPlatformAsync(
                GetCurrentUserId(),
                id,
                null,
                $"Admin đã ngừng bán cửa hàng \"{result.Name}\". Lý do: {result.StatusReason}");
            await _chatRealtime.PublishAsync(notice);
        }

        return Ok(result);
    }

    [HttpPost("{id:long}/logo")]
    [Authorize(Roles = "seller")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxLogoBytes + 1024 * 1024)]
    public async Task<ActionResult<ShopResponseDTO>> UploadLogo(long id, IFormFile file)
    {
        if (file == null || file.Length == 0 || file.Length > MaxLogoBytes)
        {
            return BadRequest(new { message = "Chọn ảnh JPG, PNG hoặc WEBP tối đa 5 MB." });
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp"
        };
        if (!allowed.Contains(file.ContentType ?? ""))
        {
            return BadRequest(new { message = "Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        {
            return BadRequest(new { message = "Định dạng ảnh không hợp lệ." });
        }

        var folder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "shops");
        Directory.CreateDirectory(folder);
        var fileName = $"{id}_{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream);
        }

        var logoUrl = $"{Request.Scheme}://{Request.Host}/uploads/shops/{fileName}";
        var result = await _shopService.SetLogoUrlAsync(GetCurrentUserId(), id, logoUrl);
        if (result == null)
        {
            System.IO.File.Delete(path);
            return NotFound(new { message = "Không tìm thấy Shop hoặc shop không thuộc về bạn." });
        }

        return Ok(result);
    }


    // =====================================================
    // GET CURRENT USER ID
    // =====================================================

    private long GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                userId,
                out var parsedUserId))
        {
            throw new UnauthorizedException(
                "Token không chứa UserId hợp lệ.");
        }

        return parsedUserId;
    }
}

public class ShopSellingRequest
{
    public bool Selling { get; set; }
    public string? Reason { get; set; }
}