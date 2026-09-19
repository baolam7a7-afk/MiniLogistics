using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using MiniLogistics.DAL.Data;

namespace MiniLogistics.DAL.Repositories;

public class Repository<T> : IRepository<T>
    where T : class
{
    protected readonly AppDbContext _context;

    protected readonly DbSet<T> _dbSet;


    public Repository(AppDbContext context)
    {
        _context = context;

        _dbSet = _context.Set<T>();
    }


    // =====================================================
    // GET ALL
    // =====================================================

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync();
    }


    // =====================================================
    // GET BY ID
    // =====================================================

    public async Task<T?> GetByIdAsync(long id)
    {
        return await _dbSet.FindAsync(id);
    }


    // =====================================================
    // FIND
    // =====================================================

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync();
    }


    // =====================================================
    // ANY
    // =====================================================

    public async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }


    // =====================================================
    // COUNT
    // =====================================================

    public async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }


    // =====================================================
    // ADD
    // =====================================================

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }


    // =====================================================
    // UPDATE
    // =====================================================

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }


    // =====================================================
    // DELETE
    // =====================================================

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}