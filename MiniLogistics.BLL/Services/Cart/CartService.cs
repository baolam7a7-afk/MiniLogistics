using MiniLogistics.BLL.DTOs.Cart;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;
using MiniLogistics.DAL.UnitOfWork;

namespace MiniLogistics.BLL.Services.Cart;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartRepository _cartRepository;


    public CartService(
        IUnitOfWork unitOfWork,
        ICartRepository cartRepository)
    {
        _unitOfWork = unitOfWork;
        _cartRepository = cartRepository;
    }


    // =====================================================
    // GET CART
    // =====================================================

    public async Task<CartResponseDto> GetCartAsync(long userId)
    {
        var cart =
            await _cartRepository
                .GetCartWithItemsByUserIdAsync(userId);

        // -------------------------------------------------
        // USER CHƯA CÓ CART
        // -------------------------------------------------

        if (cart == null)
        {
            return new CartResponseDto
            {
                UserId = userId
            };
        }

        return MapToDTO(cart);
    }


    // =====================================================
    // ADD TO CART
    // =====================================================

    public async Task<CartResponseDto> AddToCartAsync(
        long userId,
        AddToCartDto request)
    {
        // -------------------------------------------------
        // 1. VALIDATE QUANTITY
        // -------------------------------------------------

        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Quantity phải lớn hơn 0."
            );
        }


        // -------------------------------------------------
        // 2. FIND PRODUCT VARIANT
        // -------------------------------------------------

        var variant =
            await _unitOfWork.ProductVariants
                .GetByIdAsync(request.VariantId);

        if (variant == null)
        {
            throw new NotFoundException(
                "ProductVariant không tồn tại."
            );
        }


        // -------------------------------------------------
        // 3. CHECK VARIANT ACTIVE
        // -------------------------------------------------

        if (!variant.IsActive)
        {
            throw new BadRequestException(
                "ProductVariant hiện không hoạt động."
            );
        }


        // -------------------------------------------------
        // 4. CHECK STOCK
        // -------------------------------------------------

        if (variant.Stock < request.Quantity)
        {
            throw new BadRequestException(
                "Số lượng sản phẩm trong kho không đủ."
            );
        }


        // -------------------------------------------------
        // 5. FIND CART
        // -------------------------------------------------

        var cart =
            await _cartRepository
                .GetCartByUserIdAsync(userId);


        // -------------------------------------------------
        // 6. CREATE CART IF NOT EXISTS
        // -------------------------------------------------

        if (cart == null)
        {
            cart = new DAL.Models.Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Carts.AddAsync(cart);

            await _unitOfWork.SaveChangesAsync();
        }


        // -------------------------------------------------
        // 7. FIND EXISTING CART ITEM
        // -------------------------------------------------

        var cartItems =
            await _unitOfWork.CartItems.FindAsync(
                x =>
                    x.CartId == cart.Id &&
                    x.VariantId == request.VariantId
            );

        var cartItem =
            cartItems.FirstOrDefault();


        // -------------------------------------------------
        // 8. UPDATE EXISTING CART ITEM
        // -------------------------------------------------

        if (cartItem != null)
        {
            var newQuantity =
                cartItem.Quantity + request.Quantity;

            if (newQuantity > variant.Stock)
            {
                throw new BadRequestException(
                    "Số lượng trong giỏ vượt quá tồn kho."
                );
            }

            cartItem.Quantity = newQuantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.CartItems.Update(cartItem);
        }
        else
        {
            // -------------------------------------------------
            // 9. CREATE NEW CART ITEM
            // -------------------------------------------------

            cartItem = new CartItem
            {
                CartId = cart.Id,
                VariantId = request.VariantId,
                Quantity = request.Quantity,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CartItems.AddAsync(cartItem);
        }


        // -------------------------------------------------
        // 10. UPDATE CART
        // -------------------------------------------------

        cart.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Carts.Update(cart);


        // -------------------------------------------------
        // 11. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 12. GET CART AGAIN
        // -------------------------------------------------

        var result =
            await _cartRepository
                .GetCartWithItemsByUserIdAsync(userId);

        if (result == null)
        {
            throw new NotFoundException(
                "Không tìm thấy Cart sau khi thêm sản phẩm."
            );
        }


        // -------------------------------------------------
        // 13. RETURN
        // -------------------------------------------------

        return MapToDTO(result);
    }


    // =====================================================
    // UPDATE CART ITEM
    // =====================================================

    public async Task<CartResponseDto> UpdateCartItemAsync(
        long userId,
        long cartItemId,
        UpdateCartItemDto request)
    {
        // -------------------------------------------------
        // 1. VALIDATE QUANTITY
        // -------------------------------------------------

        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Quantity phải lớn hơn 0."
            );
        }


        // -------------------------------------------------
        // 2. FIND CART
        // -------------------------------------------------

        var cart =
            await _cartRepository
                .GetCartWithItemsByUserIdAsync(userId);

        if (cart == null)
        {
            throw new NotFoundException(
                "Cart không tồn tại."
            );
        }


        // -------------------------------------------------
        // 3. FIND CART ITEM
        // -------------------------------------------------

        var cartItem =
            cart.CartItems
                .FirstOrDefault(x => x.Id == cartItemId);

        if (cartItem == null)
        {
            throw new NotFoundException(
                "CartItem không tồn tại."
            );
        }


        // -------------------------------------------------
        // 4. CHECK STOCK
        // -------------------------------------------------

        if (request.Quantity >
            cartItem.Variant.Stock)
        {
            throw new BadRequestException(
                "Số lượng vượt quá tồn kho."
            );
        }


        // -------------------------------------------------
        // 5. UPDATE
        // -------------------------------------------------

        cartItem.Quantity =
            request.Quantity;

        cartItem.UpdatedAt =
            DateTime.UtcNow;

        cart.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.CartItems.Update(cartItem);
        _unitOfWork.Carts.Update(cart);


        // -------------------------------------------------
        // 6. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 7. GET CART AGAIN
        // -------------------------------------------------

        var result =
            await _cartRepository
                .GetCartWithItemsByUserIdAsync(userId);

        if (result == null)
        {
            throw new NotFoundException(
                "Không tìm thấy Cart."
            );
        }


        return MapToDTO(result);
    }


    // =====================================================
    // REMOVE CART ITEM
    // =====================================================

    public async Task<CartResponseDto> RemoveCartItemAsync(
        long userId,
        long cartItemId)
    {
        // -------------------------------------------------
        // 1. FIND CART
        // -------------------------------------------------

        var cart =
            await _cartRepository
                .GetCartWithItemsByUserIdAsync(userId);

        if (cart == null)
        {
            throw new NotFoundException(
                "Cart không tồn tại."
            );
        }


        // -------------------------------------------------
        // 2. FIND CART ITEM
        // -------------------------------------------------

        var cartItem =
            cart.CartItems
                .FirstOrDefault(x => x.Id == cartItemId);

        if (cartItem == null)
        {
            throw new NotFoundException(
                "CartItem không tồn tại."
            );
        }


        // -------------------------------------------------
        // 3. DELETE
        // -------------------------------------------------

        _unitOfWork.CartItems.Delete(cartItem);


        // -------------------------------------------------
        // 4. UPDATE CART
        // -------------------------------------------------

        cart.UpdatedAt =
            DateTime.UtcNow;

        _unitOfWork.Carts.Update(cart);


        // -------------------------------------------------
        // 5. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();


        // -------------------------------------------------
        // 6. GET CART AGAIN
        // -------------------------------------------------

        var result =
            await _cartRepository
                .GetCartWithItemsByUserIdAsync(userId);

        if (result == null)
        {
            return new CartResponseDto
            {
                UserId = userId
            };
        }


        return MapToDTO(result);
    }


    // =====================================================
    // CLEAR CART
    // =====================================================

    public async Task ClearCartAsync(long userId)
    {
        // -------------------------------------------------
        // 1. FIND CART
        // -------------------------------------------------

        var cart =
            await _cartRepository
                .GetCartByUserIdAsync(userId);

        if (cart == null)
        {
            throw new NotFoundException(
                "Cart không tồn tại."
            );
        }


        // -------------------------------------------------
        // 2. FIND CART ITEMS
        // -------------------------------------------------

        var cartItems =
            await _unitOfWork.CartItems.FindAsync(
                x => x.CartId == cart.Id
            );


        // -------------------------------------------------
        // 3. DELETE ITEMS
        // -------------------------------------------------

        foreach (var item in cartItems)
        {
            _unitOfWork.CartItems.Delete(item);
        }


        // -------------------------------------------------
        // 4. UPDATE CART
        // -------------------------------------------------

        cart.UpdatedAt =
            DateTime.UtcNow;

        _unitOfWork.Carts.Update(cart);


        // -------------------------------------------------
        // 5. SAVE
        // -------------------------------------------------

        await _unitOfWork.SaveChangesAsync();
    }


    // =====================================================
    // MAP ENTITY -> DTO
    // =====================================================

    private static CartResponseDto MapToDTO(
        DAL.Models.Cart cart)
    {
        var items =
            cart.CartItems
                .Select(item => new CartItemResponseDto
                {
                    Id = item.Id,

                    VariantId =
                        item.VariantId,

                    VariantName =
                        item.Variant.VariantName,

                    ProductId =
                        item.Variant.ProductId,

                    ProductName =
                        item.Variant.Product.Name,

                    Price =
                        item.Variant.Price,

                    Quantity =
                        item.Quantity,

                    TotalPrice =
                        item.Variant.Price *
                        item.Quantity
                })
                .ToList();


        return new CartResponseDto
        {
            Id = cart.Id,

            UserId =
                cart.UserId,

            CreatedAt =
                cart.CreatedAt,

            UpdatedAt =
                cart.UpdatedAt,

            Items =
                items,

            TotalAmount =
                items.Sum(x => x.TotalPrice),

            TotalItems =
                items.Sum(x => x.Quantity)
        };
    }
}