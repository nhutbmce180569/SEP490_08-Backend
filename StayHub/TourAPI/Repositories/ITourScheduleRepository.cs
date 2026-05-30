using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourScheduleRepository
    {
        Task<IEnumerable<TourSchedule>> GetAllAsync();
        Task<TourSchedule?> GetByIdAsync(int id);
        Task<List<TourSchedule>> GetByIdsAsync(List<int> ids);
        Task<List<TourSchedule>> GetByTourIdAsync(int tourId);
        Task AddAsync(TourSchedule tourSchedule);
        Task UpdateAsync(TourSchedule tourSchedule);
        Task DeleteAsync(TourSchedule tourSchedule);
    }
}