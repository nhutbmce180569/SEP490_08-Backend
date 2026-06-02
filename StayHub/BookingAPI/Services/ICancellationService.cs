using BookingAPI.DTOs;
using BookingAPI.Models;

namespace BookingAPI.Services
{
    public interface ICancellationService
    {
        Task<CancellationRequestDetailDTO> CreateCancellationRequestAsync(int customerId, CreateCancellationRequestDTO dto);
        Task<PaginationDTO<CancellationRequestListDTO>> GetCancellationRequestsAsync(string? status, int page, int pageSize);
        Task<CancellationRequestDetailDTO> GetCancellationRequestDetailsAsync(int id);
        Task<CancellationRequestDetailDTO> ProcessCancellationRequestAsync(int id, int processedByUserId, ProcessCancellationDTO dto);
    }
}
