using Microsoft.EntityFrameworkCore;
using MiniLogistics.BLL.DTOs.Cart;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.UnitOfWork;

using CartEntity = MiniLogistics.DAL.Models.Cart;

namespace MiniLogistics.BLL.Services.Cart;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;

    public CartService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =========================================================
    // GET MY CART
    // =========================================================
    public async Task<CartResponseDTO> GetMyCartAsync(long userId)
    {
        if (userId <= 0)
            throw new UnauthorizedAccessException("User ID không hợp lệ.");

        var cart = await GetCartEntityAsync(userId);

        if (cart == null)
        {
            return new CartResponseDTO
            {
                CartId = 0,
                UserId = userId,
                Items = new List<CartItemResponseDTO>(),
                TotalAmount = 0
            };
        }

        return MapCart(cart);
    }

    // =========================================================
    // ADD TO CART
    // =========================================================
    public async Task<CartResponseDTO> AddToCartAsync(
        long userId,
        AddToCartDTO request)
    {
        if (userId <= 0)
            throw new UnauthorizedAccessException("User ID không hợp lệ.");

        if (request == null)
            throw new BadRequestException("Dữ liệu thêm vào giỏ hàng không được để trống.");

        if (request.VariantId <= 0)
            throw new BadRequestException("VariantId không hợp lệ.");

        if (request.Quantity <= 0)
            throw new BadRequestException("Số lượng phải lớn hơn 0.");

        // -----------------------------------------------------
        // Tìm ProductVariant
        // -----------------------------------------------------
        var variant = await _unitOfWork.ProductVariants
            .Query()
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == request.VariantId);

        if (variant == null)
            throw new NotFoundException(
                $"Không tìm thấy ProductVariant với Id = {request.VariantId}.");

        if (!variant.IsActive)
            throw new BadRequestException(
                "ProductVariant hiện đang không hoạt động.");

        // -----------------------------------------------------
        // Tìm Inventory
        // -----------------------------------------------------
        var inventory = await _unitOfWork.Inventories
            .Query()
            .FirstOrDefaultAsync(
                x => x.ProductVariantId == request.VariantId);

        if (inventory == null)
            throw new NotFoundException(
                "Không tìm thấy tồn kho của sản phẩm.");

        var availableQuantity =
            inventory.Quantity - inventory.ReservedQuantity;

        if (availableQuantity <= 0)
            throw new BadRequestException(
                "Sản phẩm hiện đã hết hàng.");

        // -----------------------------------------------------
        // Tìm hoặc tạo Cart
        // -----------------------------------------------------
        var cart = await _unitOfWork.Carts
            .Query()
            .Include(x => x.CartItems)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart == null)
        {
            cart = new CartEntity
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Carts.AddAsync(cart);

            await _unitOfWork.SaveChangesAsync();

            cart = await _unitOfWork.Carts
                .Query()
                .Include(x => x.CartItems)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (cart == null)
                throw new Exception("Không thể tạo giỏ hàng.");
        }

        // -----------------------------------------------------
        // Kiểm tra CartItem đã tồn tại chưa
        // -----------------------------------------------------
        var cartItem = cart.CartItems
            .FirstOrDefault(x =>
                x.VariantId == request.VariantId);

        if (cartItem == null)
        {
            if (request.Quantity > availableQuantity)
            {
                throw new BadRequestException(
                    $"Số lượng yêu cầu vượt quá tồn kho. " +
                    $"Tồn kho khả dụng: {availableQuantity}.");
            }

            cartItem = new CartItem
            {
                CartId = cart.Id,
                VariantId = request.VariantId,
                Quantity = request.Quantity
            };

            await _unitOfWork.CartItems.AddAsync(cartItem);
        }
        else
        {
            var newQuantity =
                cartItem.Quantity + request.Quantity;

            if (newQuantity > availableQuantity)
            {
                throw new BadRequestException(
                    $"Số lượng trong giỏ vượt quá tồn kho. " +
                    $"Tồn kho khả dụng: {availableQuantity}.");
            }

            cartItem.Quantity = newQuantity;

            _unitOfWork.CartItems.Update(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Carts.Update(cart);

        await _unitOfWork.SaveChangesAsync();

        // -----------------------------------------------------
        // Reload Cart
        // -----------------------------------------------------
        var updatedCart = await GetCartEntityAsync(userId);

        if (updatedCart == null)
            throw new Exception("Không thể tải lại giỏ hàng.");

        return MapCart(updatedCart);
    }

    // =========================================================
    // UPDATE CART ITEM
    // =========================================================
    public async Task<CartResponseDTO> UpdateCartItemAsync(
        long userId,
        long cartItemId,
        UpdateCartItemDTO request)
    {
        if (userId <= 0)
            throw new UnauthorizedAccessException("User ID không hợp lệ.");

        if (cartItemId <= 0)
            throw new BadRequestException("CartItemId không hợp lệ.");

        if (request == null)
            throw new BadRequestException(
                "Dữ liệu cập nhật giỏ hàng không được để trống.");

        if (request.Quantity <= 0)
            throw new BadRequestException(
                "Số lượng phải lớn hơn 0.");

        // -----------------------------------------------------
        // Tìm CartItem thuộc User
        // -----------------------------------------------------
        var cartItem =
            await _unitOfWork.CartItems
                .Query()
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == cartItemId &&
                        x.Cart.UserId == userId);

        if (cartItem == null)
            throw new NotFoundException(
                "Không tìm thấy sản phẩm trong giỏ hàng.");

        // -----------------------------------------------------
        // Tìm Inventory
        // -----------------------------------------------------
        var inventory = await _unitOfWork.Inventories
            .Query()
            .FirstOrDefaultAsync(
                x => x.ProductVariantId == cartItem.VariantId);

        if (inventory == null)
            throw new NotFoundException(
                "Không tìm thấy tồn kho của sản phẩm.");

        var availableQuantity =
            inventory.Quantity - inventory.ReservedQuantity;

        if (request.Quantity > availableQuantity)
        {
            throw new BadRequestException(
                $"Số lượng yêu cầu vượt quá tồn kho. " +
                $"Tồn kho khả dụng: {availableQuantity}.");
        }

        // -----------------------------------------------------
        // Update Quantity
        // -----------------------------------------------------
        cartItem.Quantity = request.Quantity;

        _unitOfWork.CartItems.Update(cartItem);

        cartItem.Cart.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Carts.Update(cartItem.Cart);

        await _unitOfWork.SaveChangesAsync();

        // -----------------------------------------------------
        // Reload Cart
        // -----------------------------------------------------
        var updatedCart = await GetCartEntityAsync(userId);

        if (updatedCart == null)
            throw new Exception("Không thể tải lại giỏ hàng.");

        return MapCart(updatedCart);
    }

    // =========================================================
    // REMOVE CART ITEM
    // =========================================================
    public async Task RemoveCartItemAsync(
        long userId,
        long cartItemId)
    {
        if (userId <= 0)
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");

        if (cartItemId <= 0)
            throw new BadRequestException(
                "CartItemId không hợp lệ.");

        // -----------------------------------------------------
        // BƯỚC 1:
        // Tìm CartItem trực tiếp theo ID
        // -----------------------------------------------------
        var cartItem =
            await _unitOfWork.CartItems
                .GetByIdAsync(cartItemId);

        if (cartItem == null)
        {
            throw new NotFoundException(
                "Không tìm thấy sản phẩm trong giỏ hàng.");
        }

        // -----------------------------------------------------
        // BƯỚC 2:
        // Tìm Cart mà CartItem đang thuộc về
        // -----------------------------------------------------
        var cart =
            await _unitOfWork.Carts
                .GetByIdAsync(cartItem.CartId);

        if (cart == null)
        {
            throw new NotFoundException(
                "Không tìm thấy giỏ hàng.");
        }

        // -----------------------------------------------------
        // BƯỚC 3:
        // Kiểm tra quyền sở hữu
        // -----------------------------------------------------
        if (cart.UserId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa sản phẩm trong giỏ hàng này.");
        }

        // -----------------------------------------------------
        // BƯỚC 4:
        // Xóa CartItem
        // -----------------------------------------------------
        _unitOfWork.CartItems.Delete(cartItem);

        // -----------------------------------------------------
        // BƯỚC 5:
        // Cập nhật Cart
        // -----------------------------------------------------
        cart.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Carts.Update(cart);

        // -----------------------------------------------------
        // BƯỚC 6:
        // Lưu Database
        // -----------------------------------------------------
        await _unitOfWork.SaveChangesAsync();
    }

    // =========================================================
    // CLEAR CART
    // =========================================================
    public async Task ClearCartAsync(long userId)
    {
        if (userId <= 0)
            throw new UnauthorizedAccessException(
                "User ID không hợp lệ.");

        var cart =
            await _unitOfWork.Carts
                .Query()
                .Include(x => x.CartItems)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);

        if (cart == null)
        {
            return;
        }

        if (cart.CartItems != null &&
            cart.CartItems.Any())
        {
            foreach (var item in cart.CartItems.ToList())
            {
                _unitOfWork.CartItems.Delete(item);
            }
        }

        cart.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Carts.Update(cart);

        await _unitOfWork.SaveChangesAsync();
    }

    // =========================================================
    // GET CART ENTITY
    // =========================================================
    private async Task<CartEntity?> GetCartEntityAsync(
        long userId)
    {
        return await _unitOfWork.Carts
            .Query()
            .Include(x => x.CartItems)
                .ThenInclude(x => x.Variant)
                    .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(
                x => x.UserId == userId);
    }

    // =========================================================
    // MAP CART -> DTO
    // =========================================================
    private CartResponseDTO MapCart(CartEntity cart)
    {
        var items = new List<CartItemResponseDTO>();

        if (cart.CartItems != null)
        {
            foreach (var item in cart.CartItems)
            {
                var variant = item.Variant;

                if (variant == null)
                    continue;

                var product = variant.Product;

                if (product == null)
                    continue;

                var inventory =
                    _unitOfWork.Inventories
                        .Query()
                        .FirstOrDefault(
                            x =>
                                x.ProductVariantId ==
                                item.VariantId);

                var availableQuantity = inventory == null
                    ? 0
                    : inventory.Quantity -
                      inventory.ReservedQuantity;

                var totalPrice =
                    variant.Price * item.Quantity;

                items.Add(
                    new CartItemResponseDTO
                    {
                        Id = item.Id,

                        VariantId = variant.Id,

                        ProductId = product.Id,

                        ProductName = product.Name,

                        VariantName = variant.VariantName,

                        Sku = variant.Sku,

                        Price = variant.Price,

                        Quantity = item.Quantity,

                        TotalPrice = totalPrice,

                        AvailableQuantity =
                            availableQuantity
                    });
            }
        }

        return new CartResponseDTO
        {
            CartId = cart.Id,

            UserId = cart.UserId,

            Items = items,

            TotalAmount =
                items.Sum(x => x.TotalPrice)
        };
    }
}