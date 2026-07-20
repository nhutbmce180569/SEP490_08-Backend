using BookingAPI.DTOs;
using BookingAPI.Models;

namespace BookingAPI.Services
{
    public interface ICancellationService
    {
        Task<CancellationRequestDetailDTO> CreateCancellationRequestAsync(int customerId, CreateCancellationRequestDTO dto);
        Task<PaginationDTO<CancellationRequestListDTO>> GetCancellationRequestsAsync(int operatorId, string? status, string? date, int page, int pageSize);
        Task<CancellationRequestDetailDTO> GetCancellationRequestDetailsAsync(int id, int operatorId);
        Task<CancellationRequestDetailDTO> ProcessCancellationRequestAsync(int id, int operatorId, ProcessCancellationDTO dto);
    }
}
