using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public interface IUnitOfWork
{
    // ========================================
    // USER
    // ========================================

    IRepository<User> Users { get; }


    // ========================================
    // PRODUCT
    // ========================================

    IRepository<Product> Products { get; }


    // ========================================
    // PRODUCT VARIANT
    // ========================================

    IRepository<ProductVariant> ProductVariants { get; }


    // ========================================
    // CATEGORY
    // ========================================

    IRepository<Category> Categories { get; }


    // ========================================
    // SHOP
    // ========================================

    IRepository<Shop> Shops { get; }


    // ========================================
    // CART
    // ========================================

    IRepository<Cart> Carts { get; }


    // ========================================
    // CART ITEM
    // ========================================

    IRepository<CartItem> CartItems { get; }


    // ========================================
    // SAVE
    // ========================================

    Task<int> SaveChangesAsync();
}