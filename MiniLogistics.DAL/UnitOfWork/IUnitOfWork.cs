using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public interface IUnitOfWork
{
    // ==========================================
    // USERS
    // ==========================================

    IRepository<User> Users { get; }

    IRepository<Role> Roles { get; }

    IRepository<UserRole> UserRoles { get; }

    IRepository<UserSession> UserSessions { get; }


    // ==========================================
    // ADDRESS
    // ==========================================

    IRepository<Address> Addresses { get; }


    // ==========================================
    // SHOP
    // ==========================================

    IRepository<Shop> Shops { get; }


    // ==========================================
    // CATEGORY
    // ==========================================

    IRepository<Category> Categories { get; }


    // ==========================================
    // PRODUCT
    // ==========================================

    IRepository<Product> Products { get; }

    IRepository<ProductVariant> ProductVariants { get; }


    // ==========================================
    // INVENTORY
    // ==========================================

    IRepository<Inventory> Inventories { get; }


    // ==========================================
    // ORDER
    // ==========================================

    IRepository<Order> Orders { get; }

    IRepository<OrderItem> OrderItems { get; }

    IRepository<OrderStatusLog> OrderStatusLogs { get; }

    IRepository<Shipment> Shipments { get; }

    IRepository<ShipmentEvent> ShipmentEvents { get; }


    // ==========================================
    // SAVE
    // ==========================================

    Task<int> SaveChangesAsync();


    // ==========================================
    // TRANSACTION
    // ==========================================

    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action);
}