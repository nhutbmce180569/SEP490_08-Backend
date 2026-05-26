using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class TourScheduleItineraryRepository : ITourScheduleItineraryRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public TourScheduleItineraryRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TourScheduleItinerary>> GetByScheduleIdAsync(int scheduleId)
        {
            return await _context.TourScheduleItineraries
                .Where(x => x.ScheduleId == scheduleId)
                .ToListAsync();
        }

        public async Task<TourScheduleItinerary?> GetByIdAsync(int id)
        {
            return await _context.TourScheduleItineraries.FindAsync(id);
        }

        public async Task AddAsync(TourScheduleItinerary entity)
        {
            await _context.TourScheduleItineraries.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<TourScheduleItinerary> entities)
        {
            await _context.TourScheduleItineraries.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TourScheduleItinerary entity)
        {
            _context.TourScheduleItineraries.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TourScheduleItinerary entity)
        {
            _context.TourScheduleItineraries.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> GetExistingDayNumbers(int scheduleId, List<int> dayNumbers)
        {
            return await _context.TourScheduleItineraries
                .Where(x => x.ScheduleId == scheduleId && dayNumbers.Contains(x.DayNumber))
                .Select(x => x.DayNumber)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<DateTime>> GetExistingItineraryDates(int scheduleId, List<DateTime> dates)
        {
            var dateOnlyList = dates.Select(x => x.Date).Distinct().ToList();
            return await _context.TourScheduleItineraries
                .Where(x => x.ScheduleId == scheduleId && dateOnlyList.Contains(x.ItineraryDate.Date))
                .Select(x => x.ItineraryDate.Date)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> ExistsByScheduleIdAndDayNumber(int scheduleId, int dayNumber, int? exceptId = null)
        {
            return await _context.TourScheduleItineraries
                .Where(x => x.ScheduleId == scheduleId && x.DayNumber == dayNumber && (exceptId == null || x.Id != exceptId))
                .AnyAsync();
        }

        public async Task<bool> ExistsByScheduleIdAndItineraryDate(int scheduleId, DateTime itineraryDate, int? exceptId = null)
        {
            var dateOnly = itineraryDate.Date;
            return await _context.TourScheduleItineraries
                .Where(x => x.ScheduleId == scheduleId && x.ItineraryDate.Date == dateOnly && (exceptId == null || x.Id != exceptId))
                .AnyAsync();
        }

        public async Task<bool> ExistsByScheduleDateAndStartDuration(int scheduleId, DateTime itineraryDate, TimeOnly startDuration, int? exceptId = null)
        {
            var dateOnly = itineraryDate.Date;
            return await _context.TourScheduleItineraries
                .Where(x =>
                    x.ScheduleId == scheduleId &&
                    x.ItineraryDate.Date == dateOnly &&
                    x.StartDuration == startDuration &&
                    (exceptId == null || x.Id != exceptId))
                .AnyAsync();
        }

        public async Task<bool> HasDayDateConflict(int scheduleId, int dayNumber, DateTime itineraryDate, int? exceptId = null)
        {
            var dateOnly = itineraryDate.Date;
            return await _context.TourScheduleItineraries
                .Where(x => x.ScheduleId == scheduleId && (exceptId == null || x.Id != exceptId))
                .AnyAsync(x =>
                    (x.ItineraryDate.Date == dateOnly && x.DayNumber != dayNumber) ||
                    (x.DayNumber == dayNumber && x.ItineraryDate.Date != dateOnly));
        }
    }
}
