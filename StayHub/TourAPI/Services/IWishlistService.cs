using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IWishlistService
    {
        Task<ReadWishlistItemDTO> AddToWishlistAsync(int tourId, int customerId);
        Task RemoveFromWishlistAsync(int tourId, int customerId);
        Task<List<ReadWishlistItemDTO>> GetMyWishlistAsync(int customerId);
    }
}
