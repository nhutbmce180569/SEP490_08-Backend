using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public ReviewRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Review review)
        {
            await _context.Reviews.AddAsync(review);
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.ReviewReplies)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Review?> GetByTourAndCustomerAsync(int tourId, int customerId)
        {
            return await _context.Reviews
                .Include(r => r.ReviewReplies)
                .FirstOrDefaultAsync(r => r.TourId == tourId && r.CustomerId == customerId);
        }

        public async Task<List<Review>> GetByCustomerAsync(int customerId)
        {
            return await _context.Reviews
                .Include(r => r.Tour)
                .Include(r => r.ReviewReplies)
                .Where(r => r.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<List<Review>> GetByTourAsync(int tourId)
        {
            return await _context.Reviews
                .Include(r => r.ReviewReplies)
                .Where(r => r.TourId == tourId)
                .ToListAsync();
        }

        public void Update(Review review)
        {
            _context.Reviews.Update(review);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
