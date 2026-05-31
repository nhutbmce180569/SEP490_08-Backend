using BookingAPI.DTOs;
using BookingAPI.Models;

namespace BookingAPI.Services
{
    public interface ICancellationService
    {
        Task<CancellationRequestDetailDTO> CreateCancellationRequestAsync(int customerId, CreateCancellationRequestDTO dto);
        Task<IEnumerable<CancellationRequestListDTO>> GetCancellationRequestsAsync(string? status);
        Task<CancellationRequestDetailDTO> GetCancellationRequestDetailsAsync(int id);
        Task<CancellationRequestDetailDTO> ProcessCancellationRequestAsync(int id, int processedByUserId, ProcessCancellationDTO dto);
    }
}
