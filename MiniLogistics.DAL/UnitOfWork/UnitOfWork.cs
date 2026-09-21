using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IRepository<Product>? _products;

    private IRepository<ProductVariant>? _productVariants;

    private IRepository<Category>? _categories;


    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }


    // =====================================================
    // PRODUCT
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
    // PRODUCT VARIANT
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
    // CATEGORY
    // =====================================================

    public IRepository<Category> Categories
    {
        get
        {
            return _categories ??=
                new Repository<Category>(_context);
        }
    }


    // =====================================================
    // SAVE
    // =====================================================

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}