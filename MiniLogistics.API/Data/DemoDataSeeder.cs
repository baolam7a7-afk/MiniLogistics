using Microsoft.EntityFrameworkCore;

using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Models;

namespace MiniLogistics.API.Data;

/// <summary>
/// Seeds demo catalog data (categories, shop, 20 products with images/variants/inventory).
/// Idempotent: upserts by slug and refreshes outdated images.
/// </summary>
public static class DemoDataSeeder
{
    private const string DemoSellerEmail = "seller.demo@minilogistics.local";
    private const string DemoAdminEmail = "admin.demo@minilogistics.local";
    private const string DemoShipperEmail = "shipper.demo@minilogistics.local";
    private const string DemoShopSlug = "mini-mart-demo";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        await EnsureRolesAsync(db);
        await EnsureAdminAsync(db);
        await EnsureShipperAsync(db);
        var seller = await EnsureSellerAsync(db);
        var shop = await EnsureShopAsync(db, seller.Id);
        var categories = await EnsureCategoriesAsync(db);

        await EnsureDemoProductsAsync(db, shop.Id, categories);
        await EnsureVariantOptionsAsync(db);
        await EnsureDemoVouchersAsync(db, shop.Id);
        await RepairMissingOrStaleImagesAsync(db);

        await db.SaveChangesAsync();
    }

    private static async Task EnsureRolesAsync(AppDbContext db)
    {
        var required = new[] { "customer", "seller", "admin", "shipper" };
        var existing = await db.Roles.Select(r => r.Name).ToListAsync();

        foreach (var name in required)
        {
            if (existing.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)))
                continue;

            db.Roles.Add(new Role { Name = name });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureAdminAsync(AppDbContext db)
    {
        var admin = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == DemoAdminEmail);

        if (admin is null)
        {
            admin = new User
            {
                Email = DemoAdminEmail,
                FullName = "Demo Admin",
                Phone = "0901000100",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Status = "active",
                AuthProvider = "local",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        var adminRole = await db.Roles.FirstAsync(r => r.Name == "admin");
        if (!admin.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = admin.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureShipperAsync(AppDbContext db)
    {
        var shipper = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == DemoShipperEmail);

        if (shipper is null)
        {
            shipper = new User
            {
                Email = DemoShipperEmail,
                FullName = "Demo Shipper",
                Phone = "0901000300",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Shipper@123"),
                Status = "active",
                AuthProvider = "local",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(shipper);
            await db.SaveChangesAsync();
        }

        var shipperRole = await db.Roles.FirstAsync(r => r.Name == "shipper");
        if (!shipper.UserRoles.Any(ur => ur.RoleId == shipperRole.Id))
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = shipper.Id,
                RoleId = shipperRole.Id,
                AssignedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<User> EnsureSellerAsync(AppDbContext db)
    {
        var seller = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == DemoSellerEmail);

        if (seller is null)
        {
            seller = new User
            {
                Email = DemoSellerEmail,
                FullName = "Demo Seller",
                Phone = "0901000200",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Seller@123"),
                Status = "active",
                AuthProvider = "local",
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(seller);
            await db.SaveChangesAsync();
        }
        else
        {
            seller.FullName = "Demo Seller";
            seller.Status = "active";
            seller.UpdatedAt = DateTime.UtcNow;
        }

        var sellerRole = await db.Roles.FirstAsync(r => r.Name == "seller");
        if (!seller.UserRoles.Any(ur => ur.RoleId == sellerRole.Id))
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = seller.Id,
                RoleId = sellerRole.Id,
                AssignedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        return seller;
    }

    private static async Task<Shop> EnsureShopAsync(AppDbContext db, long ownerUserId)
    {
        var shop = await db.Shops.FirstOrDefaultAsync(s => s.Slug == DemoShopSlug);

        if (shop is null)
        {
            shop = new Shop
            {
                OwnerUserId = ownerUserId,
                Name = "Mini Mart Demo",
                Slug = DemoShopSlug,
                Description = "Cửa hàng demo MiniLogistics — hàng mẫu để trải nghiệm mua sắm.",
                LogoUrl = "https://images.unsplash.com/photo-1472851294608-062f824d29cc?auto=format&fit=crop&w=200&h=200&q=80",
                Status = "approved",
                ApprovedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            db.Shops.Add(shop);
            await db.SaveChangesAsync();
        }
        else
        {
            shop.Name = "Mini Mart Demo";
            shop.Description = "Cửa hàng demo MiniLogistics — hàng mẫu để trải nghiệm mua sắm.";
            shop.LogoUrl = "https://images.unsplash.com/photo-1472851294608-062f824d29cc?auto=format&fit=crop&w=200&h=200&q=80";
            shop.Status = "approved";
            shop.ApprovedAt ??= DateTime.UtcNow;
            shop.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return shop;
    }

    private static async Task<Dictionary<string, Category>> EnsureCategoriesAsync(AppDbContext db)
    {
        var defs = new (string Slug, string Name)[]
        {
            ("dien-thoai", "Điện thoại & Phụ kiện"),
            ("laptop", "Laptop & Máy tính"),
            ("thoi-trang", "Thời trang"),
            ("gia-dung", "Gia dụng"),
            ("sach-van-phong", "Sách & Văn phòng phẩm")
        };

        var map = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);

        foreach (var (slug, name) in defs)
        {
            var cat = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug);
            if (cat is null)
            {
                cat = new Category
                {
                    Name = name,
                    Slug = slug,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Categories.Add(cat);
                await db.SaveChangesAsync();
            }
            else
            {
                cat.Name = name;
                cat.IsActive = true;
            }

            map[slug] = cat;
        }

        await db.SaveChangesAsync();
        return map;
    }

    private static async Task EnsureDemoProductsAsync(
        AppDbContext db,
        long shopId,
        Dictionary<string, Category> categories)
    {
        var catalog = BuildCatalog(categories);

        foreach (var item in catalog)
        {
            var product = await db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.Inventory)
                .FirstOrDefaultAsync(p => p.ShopId == shopId && p.Slug == item.Slug);

            if (product is null)
            {
                product = new Product
                {
                    ShopId = shopId,
                    CategoryId = item.CategoryId,
                    Name = item.Name,
                    Slug = item.Slug,
                    Description = item.Description,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };

                db.Products.Add(product);
                await db.SaveChangesAsync();

                db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Url = item.ImageUrl,
                    SortOrder = 0
                });

                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    Sku = item.Sku,
                    VariantName = item.VariantName,
                    AttributesJson = item.AttributesJson,
                    Price = item.Price,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.ProductVariants.Add(variant);
                await db.SaveChangesAsync();

                db.Inventories.Add(new Inventory
                {
                    ProductVariantId = variant.Id,
                    Quantity = item.Stock,
                    ReservedQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                product.CategoryId = item.CategoryId;
                product.Name = item.Name;
                product.Description = item.Description;
                product.Status = "active";
                product.UpdatedAt = DateTime.UtcNow;

                // Replace stale / empty images
                var images = product.ProductImages.OrderBy(i => i.SortOrder).ToList();
                if (images.Count == 0)
                {
                    db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Url = item.ImageUrl,
                        SortOrder = 0
                    });
                }
                else
                {
                    var primary = images[0];
                    if (IsStaleImageUrl(primary.Url))
                        primary.Url = item.ImageUrl;
                    else
                        primary.Url = item.ImageUrl; // force refresh demo catalog images
                }

                var variant = product.ProductVariants.FirstOrDefault();
                if (variant is null)
                {
                    variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Sku = item.Sku,
                        VariantName = item.VariantName,
                        AttributesJson = item.AttributesJson,
                        Price = item.Price,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.ProductVariants.Add(variant);
                    await db.SaveChangesAsync();

                    db.Inventories.Add(new Inventory
                    {
                        ProductVariantId = variant.Id,
                        Quantity = item.Stock,
                        ReservedQuantity = 0,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    variant.Sku = item.Sku;
                    variant.VariantName = item.VariantName;
                    variant.AttributesJson = item.AttributesJson;
                    variant.Price = item.Price;
                    variant.IsActive = true;
                    variant.UpdatedAt = DateTime.UtcNow;

                    if (variant.Inventory is null)
                    {
                        db.Inventories.Add(new Inventory
                        {
                            ProductVariantId = variant.Id,
                            Quantity = item.Stock,
                            ReservedQuantity = 0,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        variant.Inventory.Quantity = Math.Max(variant.Inventory.Quantity, item.Stock);
                        variant.Inventory.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Đảm bảo mọi sản phẩm có 2–3 variant phù hợp loại hàng. Không xóa variant cũ.
    /// </summary>
    private static async Task EnsureVariantOptionsAsync(AppDbContext db)
    {
        var products = await db.Products
            .Include(p => p.Category)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.Inventory)
            .ToListAsync();

        foreach (var product in products)
        {
            var options = ResolveOptions(product.Slug, product.Name, product.Category?.Slug);
            var existing = product.ProductVariants
                .Select(v => v.VariantName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var basePrice = product.ProductVariants.Select(v => v.Price).DefaultIfEmpty(0).First();
            var baseStock = product.ProductVariants
                .Select(v => v.Inventory?.Quantity ?? 10)
                .DefaultIfEmpty(10)
                .Max();

            // Nếu chỉ có 1 variant tên chung, đổi tên thành option đầu tiên (không đụng đơn hàng cũ vì giữ Id).
            if (product.ProductVariants.Count == 1 && !existing.Contains(options[0].Name))
            {
                var only = product.ProductVariants.First();
                only.VariantName = options[0].Name;
                only.AttributesJson = options[0].Json;
                only.Sku = await UniqueSkuAsync(db, DesiredSku(product.Slug, options[0].SkuSuffix), only.Id);
                only.UpdatedAt = DateTime.UtcNow;
                existing.Clear();
                existing.Add(options[0].Name);
            }

            var index = 0;
            foreach (var option in options)
            {
                if (existing.Contains(option.Name))
                    continue;

                if (product.ProductVariants.Count >= 3)
                    break;

                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    Sku = await UniqueSkuAsync(db, DesiredSku(product.Slug, option.SkuSuffix), null),
                    VariantName = option.Name,
                    AttributesJson = option.Json,
                    Price = basePrice + option.PriceDelta,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.ProductVariants.Add(variant);
                await db.SaveChangesAsync();

                db.Inventories.Add(new Inventory
                {
                    ProductVariantId = variant.Id,
                    Quantity = Math.Max(1, baseStock - (index * 5)),
                    ReservedQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                });

                product.ProductVariants.Add(variant);
                existing.Add(option.Name);
                index++;
            }
        }

        await db.SaveChangesAsync();
    }

    private static string DesiredSku(string slug, string suffix) =>
        $"{slug}-{suffix}".Replace("demo-", "", StringComparison.OrdinalIgnoreCase);

    private static async Task<string> UniqueSkuAsync(AppDbContext db, string desired, long? currentVariantId)
    {
        var taken = await db.ProductVariants.AnyAsync(variant =>
            variant.Sku == desired &&
            (currentVariantId == null || variant.Id != currentVariantId));

        if (!taken)
        {
            return desired;
        }

        var suffix = currentVariantId?.ToString() ?? Guid.NewGuid().ToString("N")[..6];
        return $"{desired}-{suffix}";
    }

    private static List<(string Name, string Json, string SkuSuffix, decimal PriceDelta)> ResolveOptions(
        string slug,
        string name,
        string? categorySlug)
    {
        var key = $"{slug} {name}".ToLowerInvariant();

        if (key.Contains("but-bi") || key.Contains("bút bi"))
            return Opts(("Màu xanh", "xanh", 0), ("Màu đen", "den", 0), ("Màu đỏ", "do", 2000));

        if (key.Contains("binh-giu-nhiet") || key.Contains("bình giữ nhiệt"))
            return Opts(("Đen", "den", 0), ("Trắng", "trang", 0), ("Xanh", "xanh", 6000));

        if (key.Contains("noi-com") || key.Contains("nồi cơm"))
            return Opts(("Đỏ", "do", 0), ("Đen", "den", 0), ("Trắng", "trang", 20000));

        if (key.Contains("iphone") || key.Contains("galaxy") || key.Contains("redmi") || key.Contains("điện thoại"))
            return Opts(("128GB", "128", 0), ("256GB", "256", 1500000), ("512GB", "512", 3000000));

        if (key.Contains("airpods") || key.Contains("tai nghe"))
            return Opts(("Trắng", "trang", 0), ("Đen", "den", 0), ("Xanh", "xanh", 0));

        if (key.Contains("macbook") || key.Contains("laptop") || key.Contains("vivobook") || key.Contains("xps"))
            return Opts(("8GB RAM", "8gb", 0), ("16GB RAM", "16gb", 2500000), ("32GB RAM", "32gb", 5000000));

        if (key.Contains("chuột") || key.Contains("mx-master") || key.Contains("mouse"))
            return Opts(("Đen", "den", 0), ("Xám", "xam", 0), ("Trắng", "trang", 0));

        if (key.Contains("áo") || key.Contains("ao-thun"))
            return Opts(("Đen", "den", 0), ("Trắng", "trang", 0), ("Xanh", "xanh", 0));

        if (key.Contains("jean") || key.Contains("quần"))
            return Opts(("Size 30", "30", 0), ("Size 32", "32", 0), ("Size 34", "34", 20000));

        if (key.Contains("giày") || key.Contains("sneaker"))
            return Opts(("Size 39", "39", 0), ("Size 40", "40", 0), ("Size 41", "41", 0));

        if (key.Contains("balo"))
            return Opts(("Đen", "den", 0), ("Xám", "xam", 0), ("Xanh", "xanh", 0));

        if (key.Contains("hút bụi") || key.Contains("hut-bui"))
            return Opts(("Trắng", "trang", 0), ("Đen", "den", 0), ("Xám", "xam", 50000));

        if (key.Contains("đèn") || key.Contains("den-ban"))
            return Opts(("Trắng", "trang", 0), ("Đen", "den", 0), ("Gỗ", "go", 30000));

        if (key.Contains("sách") || key.Contains("sach"))
            return Opts(("Bìa mềm", "mem", 0), ("Bìa cứng", "cung", 40000), ("Ebook code", "ebook", -50000));

        if (key.Contains("sổ") || key.Contains("so-tay"))
            return Opts(("A5 kem", "a5", 0), ("A5 trắng", "trang", 0), ("A4", "a4", 15000));

        if (key.Contains("bàn phím") || key.Contains("ban-phim"))
            return Opts(("Switch đỏ", "do", 0), ("Switch nâu", "nau", 50000), ("Switch xanh", "xanh", 80000));

        if (string.Equals(categorySlug, "thoi-trang", StringComparison.OrdinalIgnoreCase))
            return Opts(("Đen", "den", 0), ("Trắng", "trang", 0), ("Xanh", "xanh", 0));

        if (string.Equals(categorySlug, "dien-thoai", StringComparison.OrdinalIgnoreCase))
            return Opts(("Đen", "den", 0), ("Trắng", "trang", 0), ("Xanh", "xanh", 0));

        return Opts(("Tiêu chuẩn", "std", 0), ("Cao cấp", "plus", 20000), ("Giới hạn", "ltd", 50000));
    }

    private static List<(string Name, string Json, string SkuSuffix, decimal PriceDelta)> Opts(
        params (string Name, string Suffix, decimal Delta)[] items)
    {
        return items.Select(i => (
            i.Name,
            $"{{\"option\":\"{i.Name}\"}}",
            i.Suffix,
            i.Delta)).ToList();
    }

    private static async Task RepairMissingOrStaleImagesAsync(AppDbContext db)
    {
        var products = await db.Products
            .Include(p => p.ProductImages)
            .ToListAsync();

        foreach (var product in products)
        {
            var images = product.ProductImages.OrderBy(i => i.SortOrder).ToList();

            if (images.Count == 0)
            {
                db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Url = $"https://images.unsplash.com/photo-1560393464-5c69a73c5770?auto=format&fit=crop&w=800&h=800&q=80&sig={product.Id}",
                    SortOrder = 0
                });
                continue;
            }

            foreach (var image in images.Where(i => IsStaleImageUrl(i.Url)))
            {
                image.Url = $"https://images.unsplash.com/photo-1560393464-5c69a73c5770?auto=format&fit=crop&w=800&h=800&q=80&sig={product.Id}-{image.Id}";
            }
        }
    }

    private static async Task EnsureDemoVouchersAsync(AppDbContext db, long shopId)
    {
        var now = DateTime.UtcNow;

        async Task Upsert(string code, string scope, long? shop, string name, string type, decimal value, decimal? minOrder, int limit)
        {
            var existing = await db.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
            if (existing is null)
            {
                db.Vouchers.Add(new Voucher
                {
                    Scope = scope,
                    ShopId = shop,
                    Code = code,
                    Name = name,
                    Description = name,
                    DiscountType = type,
                    DiscountValue = value,
                    MaxDiscount = type == "percent" ? 100000 : null,
                    MinOrderValue = minOrder,
                    UsageLimit = limit,
                    UsedCount = 0,
                    StartAt = now.AddDays(-1),
                    EndAt = now.AddMonths(3),
                    Status = "active",
                    CreatedAt = now
                });
            }
            else
            {
                existing.Name = name;
                existing.Status = "active";
                existing.StartAt = now.AddDays(-1);
                existing.EndAt = now.AddMonths(3);
                existing.DiscountType = type;
                existing.DiscountValue = value;
                existing.MinOrderValue = minOrder;
                existing.UsageLimit = limit;
                existing.ShopId = shop;
                existing.Scope = scope;
            }
        }

        await Upsert("WELCOME10", "platform", null, "Giảm 10% toàn sàn", "percent", 10m, 100000m, 1000);
        await Upsert("FREESHIP50K", "platform", null, "Giảm 50K đơn từ 300K", "amount", 50000m, 300000m, 500);
        await Upsert("SHOP15OFF", "shop", shopId, "Shop giảm 15%", "percent", 15m, 200000m, 200);

        await db.SaveChangesAsync();
    }

    private static bool IsStaleImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        var u = url.Trim().ToLowerInvariant();
        return u is "string" or "null" or "undefined"
            || u.Contains("picsum.photos")
            || u.Contains("placeholder")
            || u.Contains("example.com")
            || u.StartsWith("data:")
            || (!u.StartsWith("http://") && !u.StartsWith("https://") && !u.StartsWith("/"));
    }

    private static List<DemoProductDef> BuildCatalog(Dictionary<string, Category> categories)
    {
        long Cat(string slug) => categories[slug].Id;

        return new List<DemoProductDef>
        {
            // Điện thoại (4)
            new("demo-iphone-15", "iPhone 15 128GB", Cat("dien-thoai"),
                "Màn hình Super Retina XDR 6.1\", chip A16 Bionic, camera kép 48MP.",
                "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-IP15-128", "128GB / Đen", """{"color":"Đen","storage":"128GB"}""", 19990000m, 35),

            new("demo-galaxy-s24", "Samsung Galaxy S24", Cat("dien-thoai"),
                "Flagship Galaxy AI, màn Dynamic AMOLED 2X, hiệu năng mạnh mẽ.",
                "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-S24-256", "256GB / Xám", """{"color":"Xám","storage":"256GB"}""", 18990000m, 40),

            new("demo-redmi-note-13", "Xiaomi Redmi Note 13", Cat("dien-thoai"),
                "Pin lớn, sạc nhanh, camera 108MP — lựa chọn phổ thông tốt.",
                "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-RN13-128", "128GB / Xanh", """{"color":"Xanh","storage":"128GB"}""", 5290000m, 80),

            new("demo-airpods-pro-2", "AirPods Pro 2", Cat("dien-thoai"),
                "Chống ồn chủ động, Spatial Audio, hộp sạc MagSafe.",
                "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-APP2", "USB-C", """{"connector":"USB-C"}""", 5490000m, 60),

            // Laptop (4)
            new("demo-macbook-air-m3", "MacBook Air M3 13\"", Cat("laptop"),
                "Chip Apple M3, thiết kế mỏng nhẹ, pin cả ngày làm việc.",
                "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-MBA-M3-8-256", "8GB / 256GB", """{"ram":"8GB","ssd":"256GB"}""", 27990000m, 20),

            new("demo-dell-xps-13", "Dell XPS 13", Cat("laptop"),
                "Màn hình InfinityEdge sắc nét, máy mỏng cho dân văn phòng.",
                "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-XPS13-16-512", "16GB / 512GB", """{"ram":"16GB","ssd":"512GB"}""", 32990000m, 15),

            new("demo-asus-vivobook-15", "ASUS VivoBook 15", Cat("laptop"),
                "Laptop học tập / văn phòng, bàn phím số, hiệu năng ổn định.",
                "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-VB15-8-512", "i5 / 8GB / 512GB", """{"cpu":"i5","ram":"8GB"}""", 14990000m, 45),

            new("demo-logitech-mx-master-3s", "Logitech MX Master 3S", Cat("laptop"),
                "Chuột công thái học, cảm biến 8K DPI, kết nối đa thiết bị.",
                "https://images.unsplash.com/photo-1527864550417-7fd91fc51a46?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-MX3S", "Graphite", """{"color":"Graphite"}""", 2490000m, 70),

            // Thời trang (4)
            new("demo-ao-thun-basic", "Áo thun cotton basic", Cat("thoi-trang"),
                "Cotton 100%, form regular, phù hợp mặc hàng ngày.",
                "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-TEE-M-WHT", "Size M / Trắng", """{"size":"M","color":"Trắng"}""", 199000m, 120),

            new("demo-quan-jean-slim", "Quần jean slim fit", Cat("thoi-trang"),
                "Denim co giãn nhẹ, form slim hiện đại.",
                "https://images.unsplash.com/photo-1542272604-787c3835535d?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-JEAN-32-BLU", "Size 32 / Xanh", """{"size":"32","color":"Xanh"}""", 449000m, 90),

            new("demo-giay-sneaker-run", "Giày sneaker Run Lite", Cat("thoi-trang"),
                "Đế êm, thoáng khí, phù hợp đi bộ và tập nhẹ.",
                "https://images.unsplash.com/photo-1542291026-7eec264c27ff?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-SNK-42-BLK", "Size 42 / Đen", """{"size":"42","color":"Đen"}""", 699000m, 55),

            new("demo-balo-laptop-15", "Balo laptop 15.6\"", Cat("thoi-trang"),
                "Ngăn chống sốc, chống nước nhẹ, nhiều ngăn tiện dụng.",
                "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-BAG-156-GRY", "Xám", """{"color":"Xám"}""", 399000m, 65),

            // Gia dụng (4)
            new("demo-noi-com-1-8l", "Nồi cơm điện 1.8L", Cat("gia-dung"),
                "Lòng nồi chống dính, chế độ giữ ấm, dung tích gia đình.",
                "https://images.unsplash.com/photo-1584269600519-112d071b35e0?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-RC-18", "1.8L", """{"capacity":"1.8L"}""", 890000m, 40),

            new("demo-may-hut-bui-mini", "Máy hút bụi cầm tay", Cat("gia-dung"),
                "Không dây, pin sạc, lọc HEPA cho căn hộ nhỏ.",
                "https://images.unsplash.com/photo-1558317374-067fb5f30001?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-VAC-MINI", "Không dây", """{"type":"cordless"}""", 1290000m, 30),

            new("demo-binh-giu-nhiet-500", "Bình giữ nhiệt 500ml", Cat("gia-dung"),
                "Inox 304, giữ nóng/lạnh đến 12 giờ.",
                "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-FLASK-500", "500ml / Bạc", """{"capacity":"500ml","color":"Bạc"}""", 259000m, 100),

            new("demo-den-ban-led", "Đèn bàn LED cảm ứng", Cat("gia-dung"),
                "3 mức sáng, cổng USB, tiết kiệm điện.",
                "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-LAMP-LED", "Trắng", """{"color":"Trắng"}""", 319000m, 75),

            // Sách & VP (4)
            new("demo-sach-clean-code", "Sách Clean Code", Cat("sach-van-phong"),
                "Cẩm nang viết code sạch — bản tiếng Việt.",
                "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-BOOK-CC", "Bìa mềm", """{"format":"paperback"}""", 289000m, 50),

            new("demo-but-bi-set-12", "Bút bi set 12 cây", Cat("sach-van-phong"),
                "Mực đều, đầu 0.5mm, phù hợp học tập và văn phòng.",
                "https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-PEN-12", "Xanh", """{"color":"Xanh","pack":"12"}""", 49000m, 200),

            new("demo-so-tay-a5", "Sổ tay A5 bìa cứng", Cat("sach-van-phong"),
                "Giấy kem chống lóa, 200 trang, bìa cứng bền.",
                "https://images.unsplash.com/photo-1531346878377-a5be20888e57?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-NOTE-A5", "A5", """{"size":"A5"}""", 79000m, 150),

            new("demo-ban-phim-co", "Bàn phím cơ TKL", Cat("sach-van-phong"),
                "Switch tactile, layout TKL, LED trắng.",
                "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=800&h=800&q=80",
                "SKU-KB-TKL", "White LED", """{"layout":"TKL","led":"white"}""", 890000m, 35),
        };
    }

    private sealed record DemoProductDef(
        string Slug,
        string Name,
        long CategoryId,
        string Description,
        string ImageUrl,
        string Sku,
        string VariantName,
        string AttributesJson,
        decimal Price,
        int Stock);
}
