using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public interface IUnitOfWork
{
    // ========================================
    // PRODUCT
    // ========================================

    IRepository<Product> Products { get; }


    // ========================================
    // PRODUCT VARIANT
    // ========================================

    IRepository<ProductVariant> ProductVariants { get; }


    // ========================================
    // SAVE
    // ========================================

    Task<int> SaveChangesAsync();
}