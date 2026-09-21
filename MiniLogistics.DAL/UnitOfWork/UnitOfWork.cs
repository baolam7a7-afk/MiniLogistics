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

    public IRepository<Product> Products =>
        _products ??=
            new Repository<Product>(_context);

    public IRepository<ProductVariant> ProductVariants =>
        _productVariants ??=
            new Repository<ProductVariant>(_context);

    public IRepository<Category> Categories =>
        _categories ??=
            new Repository<Category>(_context);

    public IRepository<Inventory> Inventories =>
        _inventories ??=
            new Repository<Inventory>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}