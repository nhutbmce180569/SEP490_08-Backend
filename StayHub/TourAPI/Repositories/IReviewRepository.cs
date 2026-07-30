using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface IReviewRepository
    {
        Task AddAsync(Review review);
        Task<Review?> GetByIdAsync(int id);
        Task<Review?> GetByTourAndCustomerAsync(int tourId, int customerId);
        Task<List<Review>> GetByCustomerAsync(int customerId, bool includeHidden = true);
        IQueryable<Review> GetBaseQueryByTour(int tourId, bool includeHidden = false);
        Task<List<Review>> GetByTourAsync(int tourId, bool includeHidden = false);
        IQueryable<Review> GetBaseQueryByManager(int managerId, bool includeHidden = true);
        void Update(Review review);
        Task SaveChangesAsync();
    }
}
