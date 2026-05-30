using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public WishlistRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Wishlist wishlist)
        {
            await _context.Wishlists.AddAsync(wishlist);
        }

        public async Task<Wishlist?> GetByCustomerAndTourAsync(int customerId, int tourId)
        {
            return await _context.Wishlists
                .Include(w => w.Tour)
                .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.TourId == tourId);
        }

        public async Task<List<Wishlist>> GetByCustomerAsync(int customerId)
        {
            return await _context.Wishlists
                .Include(w => w.Tour)
                .Where(w => w.CustomerId == customerId)
                .OrderByDescending(w => w.Id)
                .ToListAsync();
        }

        public void Remove(Wishlist wishlist)
        {
            _context.Wishlists.Remove(wishlist);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
