using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITourScheduleItineraryService
    {
        Task<IEnumerable<ReadTourScheduleItineraryDTO>> GetByScheduleId(int scheduleId);
        Task<ReadTourScheduleItineraryDTO?> GetById(int id);
        Task<ReadTourScheduleItineraryDTO> Add(CreateTourScheduleItineraryDTO dto, int operatorId);
        Task Update(int id, UpdateTourScheduleItineraryDTO dto, int operatorId);
        Task Delete(int id, int operatorId);
        Task AddBatch(CreateTourScheduleItineraryBatchDTO batch, int operatorId);
    }
}
