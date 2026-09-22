using MiniLogistics.DAL.Models;

namespace MiniLogistics.DAL.Repositories;

public interface ICartRepository
{
    Task<Cart?> GetCartByUserIdAsync(long userId);

    Task<Cart?> GetCartWithItemsByUserIdAsync(long userId);
}