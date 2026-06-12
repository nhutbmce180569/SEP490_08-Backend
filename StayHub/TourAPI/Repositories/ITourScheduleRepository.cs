using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourScheduleRepository
    {
        Task<IEnumerable<TourSchedule>> GetAllAsync(int page, int pageSize);
        Task<TourSchedule?> GetByIdAsync(int id);
        Task<List<TourSchedule>> GetByIdsAsync(List<int> ids);
        Task<List<TourSchedule>> GetByTourIdAsync(int tourId);
        Task<TourSchedule?> GetScheduleWithItineraryAsync(int scheduleId);
        Task<IEnumerable<TourSchedule>> GetByCreatedByAsync(int userId, int page, int pageSize);
        Task<int> CountByCreatedByAsync(int userId);
        Task<IEnumerable<TourSchedule>> SearchByTourNameAsync(string tourName, int page, int pageSize);
        Task<int> CountByTourNameAsync(string tourName);
        Task<int> CountAllAsync();
        Task AddAsync(TourSchedule tourSchedule);
        Task UpdateAsync(TourSchedule tourSchedule);
        Task DeleteAsync(TourSchedule tourSchedule);
    }
}