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
        Task<List<Review>> GetByCustomerAsync(int customerId);
        Task<List<Review>> GetByTourAsync(int tourId);
        void Update(Review review);
        Task SaveChangesAsync();
    }
}
