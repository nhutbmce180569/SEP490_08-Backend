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
        Task<IEnumerable<ReadReviewDTO>> GetReviewsByTourAsync(int tourId);
    }
}
