using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IReviewService
    {
        Task<ReadReviewDTO> CreateReviewAsync(CreateReviewDTO model, int customerId);
        Task<ReadReviewDTO> UpdateReviewAsync(int reviewId, UpdateReviewDTO model, int customerId);
        Task<ReadReviewDTO?> GetMyReviewByTourAsync(int tourId, int customerId);
        Task<IEnumerable<ReadReviewDTO>> GetMyReviewsAsync(int customerId);
        Task<IEnumerable<ReadReviewDTO>> GetReviewsByTourAsync(int tourId, bool includeHidden = false);
        Task<ReadReviewReplyDTO> CreateReviewReplyAsync(int staffId, CreateReviewReplyDTO model);
        Task<ReadReviewReplyDTO> UpdateReviewReplyAsync(int replyId, int staffId, UpdateReviewReplyDTO model);
        Task DeleteReviewReplyAsync(int replyId, int staffId);
        Task HideReviewAsync(int reviewId, bool hidden);
    }
}
