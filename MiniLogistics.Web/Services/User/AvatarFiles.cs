using Microsoft.AspNetCore.Components.Forms;

namespace MiniLogistics.Web.Services.User;

public sealed class AvatarSelection
{
    public byte[] Bytes { get; init; } = [];
    public string ContentType { get; init; } = "";
    public string FileName { get; init; } = "";
    public string PreviewUrl { get; init; } = "";
}

public static class AvatarFiles
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public static async Task<(AvatarSelection? File, string? Error)> ReadAsync(IBrowserFile? file)
    {
        if (file == null)
        {
            return (null, "Vui lòng chọn ảnh.");
        }

        var extension = Path.GetExtension(file.Name);
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return (null, "Định dạng ảnh không hợp lệ. Chỉ hỗ trợ JPG, JPEG, PNG hoặc WEBP.");
        }

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return (null, "Định dạng ảnh không hợp lệ. Chỉ hỗ trợ JPG, JPEG, PNG hoặc WEBP.");
        }

        if (file.Size > MaxBytes)
        {
            return (null, "Dung lượng ảnh vượt quá 5 MB.");
        }

        await using var stream = file.OpenReadStream(MaxBytes);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var bytes = memory.ToArray();
        if (bytes.Length == 0 || bytes.Length > MaxBytes)
        {
            return (null, "Dung lượng ảnh vượt quá 5 MB.");
        }

        var contentType = file.ContentType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
            ? "image/jpeg"
            : file.ContentType;
        return (new AvatarSelection
        {
            Bytes = bytes,
            ContentType = contentType,
            FileName = "avatar" + extension.ToLowerInvariant(),
            PreviewUrl = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}"
        }, null);
    }
}
