using System.Collections.Generic;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourScheduleItineraryRepository
    {
        Task<IEnumerable<TourScheduleItinerary>> GetByScheduleIdAsync(int scheduleId);
        Task<TourScheduleItinerary?> GetByIdAsync(int id);
        Task AddAsync(TourScheduleItinerary entity);
        Task AddRangeAsync(List<TourScheduleItinerary> entities);
        Task UpdateAsync(TourScheduleItinerary entity);
        Task DeleteAsync(TourScheduleItinerary entity);
        Task<List<int>> GetExistingDayNumbers(int scheduleId, List<int> dayNumbers);
        Task<List<DateTime>> GetExistingItineraryDates(int scheduleId, List<DateTime> dates);
        Task<bool> ExistsByScheduleIdAndDayNumber(int scheduleId, int dayNumber, int? exceptId = null);
        Task<bool> ExistsByScheduleIdAndItineraryDate(int scheduleId, DateTime itineraryDate, int? exceptId = null);
        Task<bool> ExistsByScheduleDateAndStartDuration(int scheduleId, DateTime itineraryDate, TimeOnly startDuration, int? exceptId = null);
        Task<bool> HasDayDateConflict(int scheduleId, int dayNumber, DateTime itineraryDate, int? exceptId = null);
    }
}
