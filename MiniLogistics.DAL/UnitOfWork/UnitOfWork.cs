using Microsoft.EntityFrameworkCore;

using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly Data.AppDbContext _context;

    public UnitOfWork(Data.AppDbContext context)
    {
        _context = context;
    }


    // =====================================================
    // USERS
    // =====================================================

    private IRepository<User>? _users;

    public IRepository<User> Users =>
        _users ??= new Repository<User>(_context);


    // =====================================================
    // ROLES
    // =====================================================

    private IRepository<Role>? _roles;

    public IRepository<Role> Roles =>
        _roles ??= new Repository<Role>(_context);


    // =====================================================
    // USER ROLES
    // =====================================================

    private IRepository<UserRole>? _userRoles;

    public IRepository<UserRole> UserRoles =>
        _userRoles ??= new Repository<UserRole>(_context);


    // =====================================================
    // USER SESSIONS
    // =====================================================

    private IRepository<UserSession>? _userSessions;

    public IRepository<UserSession> UserSessions =>
        _userSessions ??= new Repository<UserSession>(_context);


    // =====================================================
    // ADDRESSES
    // =====================================================

    private IRepository<Address>? _addresses;

    public IRepository<Address> Addresses =>
        _addresses ??= new Repository<Address>(_context);


    // =====================================================
    // SHOPS
    // =====================================================

    private IRepository<Shop>? _shops;

    public IRepository<Shop> Shops =>
        _shops ??= new Repository<Shop>(_context);


    // =====================================================
    // CATEGORIES
    // =====================================================

    private IRepository<Category>? _categories;

    public IRepository<Category> Categories =>
        _categories ??= new Repository<Category>(_context);


    // =====================================================
    // PRODUCTS
    // =====================================================

    private IRepository<Product>? _products;

    public IRepository<Product> Products =>
        _products ??= new Repository<Product>(_context);


    // =====================================================
    // PRODUCT VARIANTS
    // =====================================================

    private IRepository<ProductVariant>? _productVariants;

    public IRepository<ProductVariant> ProductVariants =>
        _productVariants ??= new Repository<ProductVariant>(_context);


    // =====================================================
    // INVENTORIES
    // =====================================================

    private IRepository<Inventory>? _inventories;

    public IRepository<Inventory> Inventories =>
        _inventories ??= new Repository<Inventory>(_context);


    // =====================================================
    // ORDERS
    // =====================================================

    private IRepository<Order>? _orders;

    public IRepository<Order> Orders =>
        _orders ??= new Repository<Order>(_context);


    // =====================================================
    // ORDER ITEMS
    // =====================================================

    private IRepository<OrderItem>? _orderItems;

    public IRepository<OrderItem> OrderItems =>
        _orderItems ??= new Repository<OrderItem>(_context);


    // =====================================================
    // ORDER STATUS LOGS
    // =====================================================

    private IRepository<OrderStatusLog>? _orderStatusLogs;

    public IRepository<OrderStatusLog> OrderStatusLogs =>
        _orderStatusLogs ??= new Repository<OrderStatusLog>(_context);


    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }


    // =====================================================
    // TRANSACTION
    // =====================================================

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var result = await action();

            await transaction.CommitAsync();

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }
}