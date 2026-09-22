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


    // =====================================================
    // GET MY CART
    // =====================================================

    public async Task<CartResponseDTO> GetMyCartAsync(
        long userId)
    {
        var cart =
            await GetCartEntityAsync(userId);

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


    // =====================================================
    // ADD TO CART
    // =====================================================

    public async Task<CartResponseDTO> AddToCartAsync(
        long userId,
        AddToCartDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }


        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng phải lớn hơn 0.");
        }


        // -------------------------------------------------
        // 1. KIỂM TRA PRODUCT VARIANT
        // -------------------------------------------------

        var variant =
            await _unitOfWork.ProductVariants
                .Query()
                .Include(x => x.Product)
                .FirstOrDefaultAsync(
                    x => x.Id == request.VariantId);


        if (variant == null)
        {
            throw new NotFoundException(
                "Không tìm thấy ProductVariant.");
        }


        // -------------------------------------------------
        // 2. KIỂM TRA VARIANT ACTIVE
        // -------------------------------------------------

        if (!variant.IsActive)
        {
            throw new BadRequestException(
                "Sản phẩm này hiện không hoạt động.");
        }


        // -------------------------------------------------
        // 3. KIỂM TRA INVENTORY
        // -------------------------------------------------

        var inventory =
            await _unitOfWork.Inventories
                .Query()
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductVariantId ==
                        variant.Id);


        if (inventory == null)
        {
            throw new NotFoundException(
                "Không tìm thấy tồn kho của sản phẩm.");
        }


        // Available =
        // Quantity - ReservedQuantity

        int availableQuantity =
            inventory.Quantity -
            inventory.ReservedQuantity;


        if (availableQuantity <= 0)
        {
            throw new BadRequestException(
                "Sản phẩm hiện đã hết hàng.");
        }


        // -------------------------------------------------
        // 4. LẤY CART CỦA USER
        // -------------------------------------------------

        var cart =
            await _unitOfWork.Carts
                .Query()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);


        // -------------------------------------------------
        // USER CHƯA CÓ CART
        // -------------------------------------------------

        if (cart == null)
        {
            cart = new CartEntity
            {
                UserId = userId,

                CreatedAt = DateTime.UtcNow
            };


            await _unitOfWork.Carts
                .AddAsync(cart);


            await _unitOfWork.SaveChangesAsync();
        }


        // -------------------------------------------------
        // 5. KIỂM TRA CART ITEM
        // -------------------------------------------------

        var cartItem =
            await _unitOfWork.CartItems
                .Query()
                .FirstOrDefaultAsync(
                    x =>
                        x.CartId == cart.Id &&
                        x.VariantId == variant.Id);


        // =================================================
        // TRƯỜNG HỢP 1:
        // CHƯA CÓ SẢN PHẨM TRONG CART
        // =================================================

        if (cartItem == null)
        {
            if (request.Quantity >
                availableQuantity)
            {
                throw new BadRequestException(
                    $"Số lượng yêu cầu vượt quá tồn kho. " +
                    $"Chỉ còn {availableQuantity} sản phẩm.");
            }


            cartItem = new CartItem
            {
                CartId = cart.Id,

                VariantId = variant.Id,

                Quantity = request.Quantity,

                CreatedAt = DateTime.UtcNow
            };


            await _unitOfWork.CartItems
                .AddAsync(cartItem);
        }


        // =================================================
        // TRƯỜNG HỢP 2:
        // SẢN PHẨM ĐÃ CÓ TRONG CART
        // =================================================

        else
        {
            int newQuantity =
                cartItem.Quantity +
                request.Quantity;


            if (newQuantity >
                availableQuantity)
            {
                throw new BadRequestException(
                    $"Số lượng trong giỏ vượt quá tồn kho. " +
                    $"Bạn chỉ có thể có tối đa " +
                    $"{availableQuantity} sản phẩm.");
            }


            cartItem.Quantity =
                newQuantity;


            cartItem.UpdatedAt =
                DateTime.UtcNow;


            _unitOfWork.CartItems
                .Update(cartItem);
        }


        // -------------------------------------------------
        // 6. UPDATE CART
        // -------------------------------------------------

        cart.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Carts
            .Update(cart);


        // -------------------------------------------------
        // 7. SAVE
        // -------------------------------------------------

        await _unitOfWork
            .SaveChangesAsync();


        // -------------------------------------------------
        // 8. LOAD CART LẠI
        // -------------------------------------------------

        var result =
            await GetCartEntityAsync(userId);


        return MapCart(result!);
    }


    // =====================================================
    // UPDATE CART ITEM
    // =====================================================

    public async Task<CartResponseDTO>
        UpdateCartItemAsync(
            long userId,
            long cartItemId,
            UpdateCartItemDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Request không được để trống.");
        }


        if (request.Quantity <= 0)
        {
            throw new BadRequestException(
                "Số lượng phải lớn hơn 0.");
        }


        // -------------------------------------------------
        // TÌM CART ITEM
        // -------------------------------------------------

        var cartItem =
            await _unitOfWork.CartItems
                .Query()
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == cartItemId &&
                        x.Cart.UserId == userId);


        if (cartItem == null)
        {
            throw new NotFoundException(
                "Không tìm thấy sản phẩm trong giỏ hàng.");
        }


        // -------------------------------------------------
        // LẤY INVENTORY
        // -------------------------------------------------

        var inventory =
            await _unitOfWork.Inventories
                .Query()
                .FirstOrDefaultAsync(
                    x =>
                        x.ProductVariantId ==
                        cartItem.VariantId);


        if (inventory == null)
        {
            throw new NotFoundException(
                "Không tìm thấy tồn kho.");
        }


        int availableQuantity =
            inventory.Quantity -
            inventory.ReservedQuantity;


        if (request.Quantity >
            availableQuantity)
        {
            throw new BadRequestException(
                $"Số lượng yêu cầu vượt quá tồn kho. " +
                $"Chỉ còn {availableQuantity} sản phẩm.");
        }


        // -------------------------------------------------
        // UPDATE CART ITEM
        // -------------------------------------------------

        cartItem.Quantity =
            request.Quantity;


        cartItem.UpdatedAt =
            DateTime.UtcNow;


        // -------------------------------------------------
        // UPDATE CART
        // -------------------------------------------------

        cartItem.Cart.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.CartItems
            .Update(cartItem);


        _unitOfWork.Carts
            .Update(cartItem.Cart);


        await _unitOfWork
            .SaveChangesAsync();


        // -------------------------------------------------
        // LOAD CART LẠI
        // -------------------------------------------------

        var cart =
            await GetCartEntityAsync(userId);


        return MapCart(cart!);
    }


    // =====================================================
    // REMOVE CART ITEM
    // =====================================================

    public async Task RemoveCartItemAsync(
        long userId,
        long cartItemId)
    {
        var cartItem =
            await _unitOfWork.CartItems
                .Query()
                .Include(x => x.Cart)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == cartItemId &&
                        x.Cart.UserId == userId);


        if (cartItem == null)
        {
            throw new NotFoundException(
                "Không tìm thấy sản phẩm trong giỏ hàng.");
        }


        cartItem.Cart.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.CartItems
            .Delete(cartItem);


        _unitOfWork.Carts
            .Update(cartItem.Cart);


        await _unitOfWork
            .SaveChangesAsync();
    }


    // =====================================================
    // CLEAR CART
    // =====================================================

    public async Task ClearCartAsync(
        long userId)
    {
        var cart =
            await _unitOfWork.Carts
                .Query()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId);


        if (cart == null)
        {
            return;
        }


        var cartItems =
            await _unitOfWork.CartItems
                .Query()
                .Where(
                    x =>
                        x.CartId ==
                        cart.Id)
                .ToListAsync();


        foreach (var item in cartItems)
        {
            _unitOfWork.CartItems
                .Delete(item);
        }


        cart.UpdatedAt =
            DateTime.UtcNow;


        _unitOfWork.Carts
            .Update(cart);


        await _unitOfWork
            .SaveChangesAsync();
    }


    // =====================================================
    // GET CART ENTITY
    // =====================================================

    private async Task<CartEntity?>
        GetCartEntityAsync(
            long userId)
    {
        return await _unitOfWork.Carts
            .Query()

            .Include(x => x.CartItems)

                .ThenInclude(x => x.Variant)

                    .ThenInclude(x => x.Product)

            .Include(x => x.CartItems)

                .ThenInclude(x => x.Variant)

                    .ThenInclude(x => x.Inventory)

            .FirstOrDefaultAsync(
                x => x.UserId == userId);
    }


    // =====================================================
    // MAP CART → DTO
    // =====================================================

    private CartResponseDTO MapCart(
        CartEntity cart)
    {
        var items =
            cart.CartItems
                .Select(item =>
                {
                    int availableQuantity =
                        item.Variant.Inventory.Quantity -
                        item.Variant.Inventory.ReservedQuantity;


                    return new CartItemResponseDTO
                    {
                        Id =
                            item.Id,

                        VariantId =
                            item.VariantId,

                        ProductId =
                            item.Variant.ProductId,

                        ProductName =
                            item.Variant.Product.Name,

                        VariantName =
                            item.Variant.VariantName,

                        Sku =
                            item.Variant.Sku,

                        Price =
                            item.Variant.Price,

                        Quantity =
                            item.Quantity,

                        TotalPrice =
                            item.Variant.Price *
                            item.Quantity,

                        AvailableQuantity =
                            availableQuantity
                    };
                })
                .ToList();


        return new CartResponseDTO
        {
            CartId =
                cart.Id,

            UserId =
                cart.UserId,

            Items =
                items,

            TotalAmount =
                items.Sum(
                    x => x.TotalPrice)
        };
    }
}