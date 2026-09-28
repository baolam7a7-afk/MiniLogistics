
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/users/me/avatar")]
[Authorize]
public class AvatarController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    public AvatarController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "Vui lòng chọn ảnh để upload."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message = "Dung lượng ảnh tối đa là 5 MB."
            });
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP."
            });
        }

        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdValue, out var userId) ||
            userId <= 0)
        {
            return Unauthorized(new
            {
                message = "Không xác định được người dùng."
            });
        }

        // Sử dụng thống nhất thư mục wwwroot bên trong ContentRootPath.
        var webRoot = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot");

        var uploadFolder = Path.Combine(
            webRoot,
            "uploads",
            "avatars");

        Directory.CreateDirectory(uploadFolder);

        var fileName =
            $"{userId}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var filePath = Path.Combine(uploadFolder, fileName);

        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await file.CopyToAsync(stream, cancellationToken);
        }
        catch
        {
            // Xóa file chưa hoàn chỉnh nếu quá trình ghi thất bại.
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            throw;
        }

        // Kiểm tra file thực sự đã được tạo.
        if (!System.IO.File.Exists(filePath))
        {
            return StatusCode(500, new
            {
                message = "Không tìm thấy file ảnh sau khi upload."
            });
        }

        var avatarUrl =
            $"{Request.Scheme}://{Request.Host}/uploads/avatars/{fileName}";

        return Ok(new
        {
            avatarUrl,
            message = "Upload ảnh đại diện thành công."
        });
    }
}