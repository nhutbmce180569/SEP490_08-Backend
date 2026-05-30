using Microsoft.EntityFrameworkCore;
using TourAPI.Models;

namespace TourAPI.Repositories.Implements
{
    public class TourRepository : ITourRepository
    {
        private readonly StayHubCatalogDbContext _context;

        public TourRepository(StayHubCatalogDbContext context)
        {
            _context = context;
        }
        public async Task Add(Tour model)
        {
            await _context.Tours.AddAsync(model);
        }

        public async Task Delete(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.Reviews)
                .Include(t => t.TourItineraries)
                .Include(t => t.TourSchedules)
                .Include(t => t.Wishlists)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tour == null)
                return;

            // remove child tables
            _context.Reviews.RemoveRange(tour.Reviews);

            _context.TourItineraries.RemoveRange(tour.TourItineraries);

            _context.TourSchedules.RemoveRange(tour.TourSchedules);

            _context.Wishlists.RemoveRange(tour.Wishlists);

            // remove parent
            _context.Tours.Remove(tour);

            await _context.SaveChangesAsync();
        }

        public async Task<List<Tour>> GetAll()
        {
            try
            {
                return await _context.Tours
                    .Include(t => t.TourItineraries)
                    .Include(t => t.TourSchedules)
                        .ThenInclude(t => t.TourScheduleTickets)
                    .Include(t => t.Reviews)
                        .ThenInclude(r => r.ReviewReplies)
                    .ToListAsync();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        public async Task<List<Tour>> GetActiveTours()
        {
            try
            {
                return await _context.Tours
                    .Include(t => t.TourItineraries)
                    .Include(t => t.TourSchedules)
                        .ThenInclude(t => t.TourScheduleTickets)
                    .Include(t => t.Reviews)
                        .ThenInclude(r => r.ReviewReplies)
                    .Where(t => t.Status == "Active")
                    .ToListAsync();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<Tour> GetById(int id)
        {
            try
            {
                return await _context.Tours
                    .Include(t => t.TourItineraries.OrderBy(x => x.DayNumber))
                    .Include(t => t.TourSchedules)
                        .ThenInclude(t => t.TourScheduleTickets)
                    .Include(t => t.Reviews)
                        .ThenInclude(r => r.ReviewReplies)
                    .FirstOrDefaultAsync(g => g.Id == id);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

        }

        public void Update(Tour model)
        {
            _context.Tours.Update(model);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountByCategoryIdAsync(int categoryId)
        {
            try
            {
                // Đếm số lượng tour có CategoryId truyền vào
                return await _context.Tours.CountAsync(t => t.CategoryId == categoryId);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
