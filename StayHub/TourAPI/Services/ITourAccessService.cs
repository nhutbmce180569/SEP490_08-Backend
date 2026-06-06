namespace TourAPI.Services;

public interface ITourAccessService
{
    Task<bool> CanEditAsync(int tourId, int userId, bool isAdmin);
    Task<bool> CanEditScheduleAsync(int scheduleId, int userId, bool isAdmin);
    Task<bool> CanManageTourAsync(int tourId, int userId, bool isAdmin, bool isStaff);
    Task<bool> CanManageReviewAsync(int reviewId, int userId, bool isAdmin, bool isStaff);
    Task<bool> CanManageReviewReplyAsync(int replyId, int userId, bool isAdmin, bool isStaff);
}
