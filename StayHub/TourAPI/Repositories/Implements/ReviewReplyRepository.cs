using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class ReviewReplyRepository : IReviewReplyRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public ReviewReplyRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ReviewReply reviewReply)
        {
            await _context.ReviewReplies.AddAsync(reviewReply);
        }

        public async Task<ReviewReply?> GetByIdAsync(int id)
        {
            return await _context.ReviewReplies.FirstOrDefaultAsync(r => r.Id == id);
        }

        public void Update(ReviewReply reviewReply)
        {
            _context.ReviewReplies.Update(reviewReply);
        }

        public async Task DeleteAsync(ReviewReply reviewReply)
        {
            _context.ReviewReplies.Remove(reviewReply);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
