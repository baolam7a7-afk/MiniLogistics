using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }

    IRepository<Role> Roles { get; }

    IRepository<UserRole> UserRoles { get; }

    IRepository<UserSession> UserSessions { get; }

    IRepository<Category> Categories { get; }

    IRepository<Product> Products { get; }

    IRepository<ProductVariant> ProductVariants { get; }

    IRepository<Inventory> Inventories { get; }

    Task<int> SaveChangesAsync();
}