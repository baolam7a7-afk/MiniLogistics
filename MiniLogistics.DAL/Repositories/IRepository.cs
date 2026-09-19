using System.Linq.Expressions;

namespace MiniLogistics.DAL.Repositories;

public interface IRepository<T>
    where T : class
{
    // Lấy tất cả
    Task<IEnumerable<T>> GetAllAsync();

    // Lấy theo ID
    Task<T?> GetByIdAsync(long id);

    // Tìm theo điều kiện
    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate
    );

    // Kiểm tra tồn tại
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate
    );

    // Đếm
    Task<int> CountAsync(
        Expression<Func<T, bool>> predicate
    );

    // Thêm
    Task AddAsync(T entity);

    // Cập nhật
    void Update(T entity);

    // Xóa
    void Delete(T entity);
}