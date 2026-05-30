using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface IWishlistRepository
    {
        Task AddAsync(Wishlist wishlist);
        Task<Wishlist?> GetByCustomerAndTourAsync(int customerId, int tourId);
        Task<List<Wishlist>> GetByCustomerAsync(int customerId);
        void Remove(Wishlist wishlist);
        Task SaveChangesAsync();
    }
}
