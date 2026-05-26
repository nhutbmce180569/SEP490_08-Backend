using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITourItineraryService
    {
        Task<PaginationDTO<ReadTourItineraryDTO>> GetAll(int page, int pageSize);
        Task<ReadTourItineraryDTO?> GetById(int id);
        Task Add(CreateTourItineraryDTO dto, int operatorId);
        Task Update(int id, UpdateTourItineraryDTO dto, int operatorId);
        Task Delete(int id);
        Task AddBatch(CreateTourItineraryBatchDTO batch, int operatorId);
    }
}