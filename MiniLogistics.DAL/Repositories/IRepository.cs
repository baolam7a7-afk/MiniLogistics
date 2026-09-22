using System.Linq.Expressions;

namespace MiniLogistics.DAL.Repositories;

public interface IRepository<T>
    where T : class
{
    Task<IEnumerable<T>> GetAllAsync();

    Task<T?> GetByIdAsync(long id);

    Task<T?> GetByIdAsync(int id);

    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate
    );

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate
    );

    Task<int> CountAsync(
        Expression<Func<T, bool>> predicate
    );

    Task AddAsync(T entity);

    void Update(T entity);

    void Delete(T entity);
}