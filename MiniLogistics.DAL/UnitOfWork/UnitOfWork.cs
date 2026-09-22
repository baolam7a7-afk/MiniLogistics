using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // ========================================
    // REPOSITORY FIELDS
    // ========================================

    private IRepository<User>? _users;

    private IRepository<Product>? _products;

    private IRepository<ProductVariant>? _productVariants;

    private IRepository<Category>? _categories;

    private IRepository<Shop>? _shops;

    private IRepository<Cart>? _carts;

    private IRepository<CartItem>? _cartItems;


    // ========================================
    // CONSTRUCTOR
    // ========================================

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }


    // ========================================
    // USER
    // ========================================

    public IRepository<User> Users
    {
        get
        {
            return _users ??=
                new Repository<User>(_context);
        }
    }


    // ========================================
    // PRODUCT
    // ========================================

    public IRepository<Product> Products
    {
        get
        {
            return _products ??=
                new Repository<Product>(_context);
        }
    }


    // ========================================
    // PRODUCT VARIANT
    // ========================================

    public IRepository<ProductVariant> ProductVariants
    {
        get
        {
            return _productVariants ??=
                new Repository<ProductVariant>(_context);
        }
    }


    // ========================================
    // CATEGORY
    // ========================================

    public IRepository<Category> Categories
    {
        get
        {
            return _categories ??=
                new Repository<Category>(_context);
        }
    }


    // ========================================
    // SHOP
    // ========================================

    public IRepository<Shop> Shops
    {
        get
        {
            return _shops ??=
                new Repository<Shop>(_context);
        }
    }


    // ========================================
    // CART
    // ========================================

    public IRepository<Cart> Carts
    {
        get
        {
            return _carts ??=
                new Repository<Cart>(_context);
        }
    }


    // ========================================
    // CART ITEM
    // ========================================

    public IRepository<CartItem> CartItems
    {
        get
        {
            return _cartItems ??=
                new Repository<CartItem>(_context);
        }
    }


    // ========================================
    // SAVE
    // ========================================

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}