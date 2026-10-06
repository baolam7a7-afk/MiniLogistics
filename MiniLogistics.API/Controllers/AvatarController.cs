using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniLogistics.BLL.Services.User;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.API.Controllers;

[ApiController]
[Route("api/users/me/avatar")]
[Authorize]
public class AvatarController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IUserService _users;
    private readonly IUnitOfWork _unitOfWork;

    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp"
        };

    public AvatarController(
        IWebHostEnvironment environment,
        IUserService users,
        IUnitOfWork unitOfWork)
    {
        _environment = environment;
        _users = users;
        _unitOfWork = unitOfWork;
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
            return BadRequest(new { message = "Vui lòng chọn ảnh để upload." });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "Dung lượng ảnh tối đa là 5 MB." });
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            !AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Chỉ chấp nhận ảnh JPG, JPEG, PNG hoặc WEBP." });
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId) || userId <= 0)
        {
            return Unauthorized(new { message = "Không xác định được người dùng." });
        }

        byte[] bytes;
        await using (var input = file.OpenReadStream())
        {
            using var memory = new MemoryStream();
            await input.CopyToAsync(memory, cancellationToken);
            bytes = memory.ToArray();
        }

        if (bytes.Length == 0 || bytes.Length > MaxFileSize)
        {
            return BadRequest(new { message = "Dung lượng ảnh tối đa là 5 MB." });
        }

        var extension = DetectImageExtension(bytes);
        if (extension == null)
        {
            return BadRequest(new { message = "File không phải ảnh JPG, JPEG, PNG hoặc WEBP." });
        }

        var webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadFolder = Path.GetFullPath(Path.Combine(webRoot, "uploads", "avatars"));
        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{userId}_{Guid.NewGuid():N}{extension}";
        var filePath = Path.GetFullPath(Path.Combine(uploadFolder, fileName));
        if (!filePath.StartsWith(uploadFolder, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Tên file không hợp lệ." });
        }

        try
        {
            await System.IO.File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
        }
        catch
        {
            DeleteFile(filePath);
            throw;
        }

        var avatarUrl = $"{Request.Scheme}://{Request.Host}/uploads/avatars/{fileName}";
        string? previous;
        try
        {
            previous = await _users.ReplaceAvatarUrlAsync(userId, avatarUrl);
        }
        catch
        {
            DeleteFile(filePath);
            throw;
        }

        await DeletePreviousAsync(previous, avatarUrl, userId, uploadFolder);
        return Ok(new
        {
            avatarUrl,
            message = "Upload ảnh đại diện thành công."
        });
    }

    private async Task DeletePreviousAsync(
        string? previous,
        string newUrl,
        long userId,
        string uploadFolder)
    {
        if (string.IsNullOrWhiteSpace(previous) ||
            string.Equals(previous, newUrl, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!TryMapOwnedFile(previous, userId, uploadFolder, out var fullPath))
        {
            return;
        }

        var shared = await _unitOfWork.Users.AnyAsync(user =>
            user.Id != userId && user.AvatarUrl == previous);
        if (shared || !System.IO.File.Exists(fullPath))
        {
            return;
        }

        DeleteFile(fullPath);
    }

    private static bool TryMapOwnedFile(
        string avatarUrl,
        long userId,
        string uploadFolder,
        out string fullPath)
    {
        fullPath = "";
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var name = Path.GetFileName(uri.AbsolutePath);
        if (!Regex.IsMatch(
                name,
                $"^{userId}_[a-fA-F0-9]{{32}}\\.(jpg|jpeg|png|webp)$",
                RegexOptions.IgnoreCase))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(uploadFolder, name));
        if (!candidate.StartsWith(uploadFolder, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private static string? DetectImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xD8 &&
            bytes[2] == 0xFF)
        {
            return ".jpg";
        }

        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 &&
            bytes[1] == 0x50 &&
            bytes[2] == 0x4E &&
            bytes[3] == 0x47 &&
            bytes[4] == 0x0D &&
            bytes[5] == 0x0A &&
            bytes[6] == 0x1A &&
            bytes[7] == 0x0A)
        {
            return ".png";
        }

        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 &&
            bytes[1] == 0x49 &&
            bytes[2] == 0x46 &&
            bytes[3] == 0x46 &&
            bytes[8] == 0x57 &&
            bytes[9] == 0x45 &&
            bytes[10] == 0x42 &&
            bytes[11] == 0x50)
        {
            return ".webp";
        }

        return null;
    }

    private static void DeleteFile(string path)
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
}
