using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;


    // Repository Product
    private IRepository<Product>? _products;


    // Repository ProductVariant
    private IRepository<ProductVariant>? _productVariants;


    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }


    // =====================================================
    // PRODUCTS
    // =====================================================

    public IRepository<Product> Products
    {
        get
        {
            return _products ??=
                new Repository<Product>(_context);
        }
    }


    // =====================================================
    // PRODUCT VARIANTS
    // =====================================================

    public IRepository<ProductVariant> ProductVariants
    {
        get
        {
            return _productVariants ??=
                new Repository<ProductVariant>(_context);
        }
    }


    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}