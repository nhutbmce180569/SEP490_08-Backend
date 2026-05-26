using Microsoft.EntityFrameworkCore;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class TourItineraryRepository : ITourItineraryRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public TourItineraryRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TourItinerary>> GetAllAsync()
        {
            return await _context.TourItineraries.ToListAsync();
        }

        public async Task<TourItinerary?> GetByIdAsync(int id)
        {
            return await _context.TourItineraries.FindAsync(id);
        }

        public async Task AddAsync(TourItinerary tourItinerary)
        {
            await _context.TourItineraries.AddAsync(tourItinerary);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TourItinerary tourItinerary)
        {
            _context.TourItineraries.Update(tourItinerary);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TourItinerary tourItinerary)
        {
            _context.TourItineraries.Remove(tourItinerary);
            await _context.SaveChangesAsync();
        }
        public async Task AddRange(List<TourItinerary> itineraries)
        {
            await _context.TourItineraries.AddRangeAsync(itineraries);
            await _context.SaveChangesAsync();

        }
        public async Task<List<TourItinerary>> GetByIds(List<int> ids)
        {
            return await _context.TourItineraries
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
        }

        public async Task<List<int>> GetExistingDayNumbers(int tourId, List<int> dayNumbers)
        {
            return await _context.TourItineraries
                .Where(x => x.TourId == tourId && dayNumbers.Contains(x.DayNumber))
                .Select(x => x.DayNumber)
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool> ExistsByTourIdAndDayNumber(int tourId, int dayNumber, int? exceptId = null)
        {
            return await _context.TourItineraries
                .Where(x => x.TourId == tourId && x.DayNumber == dayNumber && (exceptId == null || x.Id != exceptId))
                .AnyAsync();
        }

        public async Task<bool> ExistsByTourDayAndStartDuration(int tourId, int dayNumber, TimeOnly startDuration, int? exceptId = null)
        {
            return await _context.TourItineraries
                .Where(x =>
                    x.TourId == tourId &&
                    x.DayNumber == dayNumber &&
                    x.StartDuration == startDuration &&
                    (exceptId == null || x.Id != exceptId))
                .AnyAsync();
        }

        public async Task<int> SaveChanges()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
