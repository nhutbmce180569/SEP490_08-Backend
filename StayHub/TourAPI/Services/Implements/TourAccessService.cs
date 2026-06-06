using Microsoft.EntityFrameworkCore;
using TourAPI.Models;

namespace TourAPI.Services.Implements;

public class TourAccessService : ITourAccessService
{
    private readonly StayHubCatalogDbContext _context;

    public TourAccessService(StayHubCatalogDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanEditAsync(int tourId, int userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return await _context.Tours.AnyAsync(x => x.Id == tourId);
        }

        return await _context.Tours.AnyAsync(x =>
            x.Id == tourId && x.CreatedBy == userId);
    }

    public async Task<bool> CanEditScheduleAsync(int scheduleId, int userId, bool isAdmin)
    {
        return await _context.TourSchedules
            .Where(schedule => schedule.Id == scheduleId)
            .AnyAsync(schedule =>
                isAdmin || schedule.Tour.CreatedBy == userId);
    }

    public async Task<bool> CanManageTourAsync(
        int tourId,
        int userId,
        bool isAdmin,
        bool isStaff)
    {
        if (isAdmin)
        {
            return await _context.Tours.AnyAsync(tour => tour.Id == tourId);
        }

        if (await _context.Tours.AnyAsync(tour =>
                tour.Id == tourId && tour.CreatedBy == userId))
        {
            return true;
        }

        return isStaff && await _context.TourScheduleStaffs.AnyAsync(assignment =>
            assignment.StaffId == userId &&
            assignment.Schedule.TourId == tourId);
    }

    public async Task<bool> CanManageReviewAsync(
        int reviewId,
        int userId,
        bool isAdmin,
        bool isStaff)
    {
        var tourId = await _context.Reviews
            .Where(review => review.Id == reviewId)
            .Select(review => (int?)review.TourId)
            .FirstOrDefaultAsync();

        return tourId.HasValue &&
               await CanManageTourAsync(tourId.Value, userId, isAdmin, isStaff);
    }

    public async Task<bool> CanManageReviewReplyAsync(
        int replyId,
        int userId,
        bool isAdmin,
        bool isStaff)
    {
        var tourId = await _context.ReviewReplies
            .Where(reply => reply.Id == replyId)
            .Select(reply => (int?)reply.Review.TourId)
            .FirstOrDefaultAsync();

        return tourId.HasValue &&
               await CanManageTourAsync(tourId.Value, userId, isAdmin, isStaff);
    }
}
