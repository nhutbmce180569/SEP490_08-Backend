using TourAPI.Models;

namespace TourAPI.Repositories
{
    public interface ITourItineraryRepository
    {
        Task<IEnumerable<TourItinerary>> GetAllAsync();
        Task<TourItinerary?> GetByIdAsync(int id);
        Task AddAsync(TourItinerary tourItinerary);
        Task UpdateAsync(TourItinerary tourItinerary);
        Task DeleteAsync(TourItinerary tourItinerary);
        Task AddRange(List<TourItinerary> itineraries);
        Task<List<TourItinerary>> GetByIds(List<int> ids);
        Task<List<int>> GetExistingDayNumbers(int tourId, List<int> dayNumbers);
        Task<bool> ExistsByTourIdAndDayNumber(int tourId, int dayNumber, int? exceptId = null);
        Task<bool> ExistsByTourDayAndStartDuration(int tourId, int dayNumber, TimeOnly startDuration, int? exceptId = null);
        Task<int> SaveChanges();
    }
}
