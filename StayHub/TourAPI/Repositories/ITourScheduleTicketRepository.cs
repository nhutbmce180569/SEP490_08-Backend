using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourScheduleTicketRepository
    {
        Task<IEnumerable<TourScheduleTicket>> GetAllAsync();
        Task<IEnumerable<TourScheduleTicket>> GetByScheduleIdAsync(int scheduleId);
        Task<TourScheduleTicket?> GetByIdAsync(int id);
        Task AddAsync(TourScheduleTicket entity);
        Task UpdateAsync(TourScheduleTicket entity);
        Task SetActiveAsync(TourScheduleTicket entity, bool isActive);
        Task<bool> ExistsByScheduleAndTicketTypeAsync(int scheduleId, int ticketTypeId, int? exceptId = null);
        Task<bool> ReserveAsync(int id, int quantity);
        Task<bool> ReleaseAsync(int id, int quantity);
    }
}
