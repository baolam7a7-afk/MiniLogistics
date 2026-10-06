using System.Globalization;
using System.Text;

namespace MiniLogistics.Web.Services.Product;

public static class VariantImages
{
    public static string Resolve(string? variantName, string? fallbackUrl)
    {
        var name = Fold(variantName);
        foreach (var rule in Rules)
        {
            if (name.Contains(rule.Key, StringComparison.Ordinal))
            {
                return rule.Url;
            }
        }

        return fallbackUrl ?? "";
    }

    private static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var form = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch == 'đ' ? 'd' : ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static readonly (string Key, string Url)[] Rules =
    [
        ("den", "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?auto=format&fit=crop&w=800&h=800&q=80"),
        ("trang", "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=800&h=800&q=80"),
        ("xam", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&h=800&q=80"),
        ("xanh", "https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?auto=format&fit=crop&w=800&h=800&q=80"),
        ("do", "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=800&h=800&q=80"),
        ("go", "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=800&h=800&q=80"),
        ("nau", "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=800&h=800&q=80"),
        ("128", "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&h=800&q=80"),
        ("256", "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?auto=format&fit=crop&w=800&h=800&q=80"),
        ("512", "https://images.unsplash.com/photo-1592899677977-9c10ca588bbd?auto=format&fit=crop&w=800&h=800&q=80"),
        ("8gb", "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=800&h=800&q=80"),
        ("16gb", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&h=800&q=80"),
        ("32gb", "https://images.unsplash.com/photo-1484788984921-03950022c9ef?auto=format&fit=crop&w=800&h=800&q=80"),
        ("size 39", "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=800&h=800&q=80"),
        ("size 40", "https://images.unsplash.com/photo-1460353581641-37baddab0c3d?auto=format&fit=crop&w=800&h=800&q=80"),
        ("size 41", "https://images.unsplash.com/photo-1549298916-b41d501d3772?auto=format&fit=crop&w=800&h=800&q=80"),
        ("size 30", "https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=800&h=800&q=80"),
        ("size 32", "https://images.unsplash.com/photo-1475178626620-a4d074967452?auto=format&fit=crop&w=800&h=800&q=80"),
        ("size 34", "https://images.unsplash.com/photo-1541099649105-f69ad21f3246?auto=format&fit=crop&w=800&h=800&q=80"),
        ("bia mem", "https://images.unsplash.com/photo-1544947950-fa07a98d237f?auto=format&fit=crop&w=800&h=800&q=80"),
        ("bia cung", "https://images.unsplash.com/photo-1512820790803-83ca734da794?auto=format&fit=crop&w=800&h=800&q=80"),
        ("ebook", "https://images.unsplash.com/photo-1519682337058-a94d519337bc?auto=format&fit=crop&w=800&h=800&q=80"),
        ("a5", "https://images.unsplash.com/photo-1517842645767-c639042777db?auto=format&fit=crop&w=800&h=800&q=80"),
        ("a4", "https://images.unsplash.com/photo-1456327102063-fb5054efe647?auto=format&fit=crop&w=800&h=800&q=80"),
        ("kem", "https://images.unsplash.com/photo-1517842645767-c639042777db?auto=format&fit=crop&w=800&h=800&q=80"),
        ("tieu chuan", "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=800&h=800&q=80"),
        ("cao cap", "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?auto=format&fit=crop&w=800&h=800&q=80"),
        ("gioi han", "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?auto=format&fit=crop&w=800&h=800&q=80")
    ];
}
