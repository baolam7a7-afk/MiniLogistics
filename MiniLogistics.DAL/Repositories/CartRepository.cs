using Microsoft.EntityFrameworkCore;
using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Models;

namespace MiniLogistics.DAL.Repositories;

public class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    public CartRepository(AppDbContext context)
    {
        _context = context;
    }


    // =====================================================
    // GET CART BY USER ID
    // =====================================================

    public async Task<Cart?> GetCartByUserIdAsync(long userId)
    {
        return await _context.Carts
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }


    // =====================================================
    // GET CART WITH ITEMS
    // =====================================================

    public async Task<Cart?> GetCartWithItemsByUserIdAsync(long userId)
    {
        return await _context.Carts
            .AsNoTracking()
            .Include(x => x.CartItems)
                .ThenInclude(x => x.Variant)
                    .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }
}