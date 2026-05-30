using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface IReviewReplyRepository
    {
        Task AddAsync(ReviewReply reviewReply);
        Task<ReviewReply?> GetByIdAsync(int id);
        void Update(ReviewReply reviewReply);
        Task DeleteAsync(ReviewReply reviewReply);
        Task SaveChangesAsync();
    }
}
