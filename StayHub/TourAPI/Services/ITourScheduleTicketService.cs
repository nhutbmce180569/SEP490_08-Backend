using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface ITourScheduleTicketService
    {
        Task<PaginationDTO<ReadTourScheduleTicketDTO>> GetAll(int page, int pageSize);
        Task<IEnumerable<ReadTourScheduleTicketDTO>> GetByScheduleId(int scheduleId);
        Task<ReadTourScheduleTicketDTO?> GetById(int id);
        Task<ReadTourScheduleTicketDTO> Create(CreateTourScheduleTicketDTO dto);
        Task<ReadTourScheduleTicketDTO> Update(int id, UpdateTourScheduleTicketDTO dto);
        Task<ReadTourScheduleTicketDTO> Activate(int id);
        Task<ReadTourScheduleTicketDTO> Deactivate(int id);
        Task<bool> Reserve(int id, int quantity);
        Task<bool> Release(int id, int quantity);
    }
}
