using Microsoft.AspNetCore.OData.Query;
using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Services
{
    public interface IReviewService
    {
        Task<ReadReviewDTO> CreateReviewAsync(CreateReviewDTO model, int customerId);
        Task<ReadReviewDTO> UpdateReviewAsync(int reviewId, UpdateReviewDTO model, int customerId);
        Task<ReadReviewDTO?> GetMyReviewByTourAsync(int tourId, int customerId);
        Task<PaginationDTO<ReadReviewDTO>> GetReviewsByManagerAsync(int managerId, int tourId, int page, int pageSize, int? rating, string sortOrder);
        Task<IEnumerable<ReadReviewDTO>> GetMyReviewsAsync(int customerId);
        Task<PaginationDTO<ReadReviewDTO>> GetReviewsByTourAsync(int tourId, int page, int pageSize, int? rating, string sortOrder, bool includeHidden = false);
        Task<ReadReviewReplyDTO> CreateReviewReplyAsync(int staffId, CreateReviewReplyDTO model);
        Task<ReadReviewReplyDTO> UpdateReviewReplyAsync(int replyId, int staffId, UpdateReviewReplyDTO model);
        Task DeleteReviewReplyAsync(int replyId, int staffId);
        Task HideReviewAsync(int reviewId, bool hidden);
    }
}
