using SocialAPI.DTOs;
using SocialAPI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialAPI.Repositories;

public interface IMomentRepository
{
    Task<TourMoment> CreateMomentAsync(TourMoment moment);
    Task<IEnumerable<TourMoment>> GetMomentsByScheduleIdAsync(int? scheduleId, int currentUserId);
    IQueryable<TourMoment> GetMomentsAsQueryable();
    Task<TourMoment?> GetMomentByIdAsync(int id);
    Task DeleteMomentAsync(TourMoment moment);

    Task<MomentReaction?> GetReactionAsync(int momentId, int userId);
    Task AddReactionAsync(MomentReaction reaction);
    Task RemoveReactionAsync(MomentReaction reaction);

    Task<MomentComment> AddCommentAsync(MomentComment comment);
    Task<MomentComment?> GetCommentByIdAsync(int commentId);
    Task UpdateCommentAsync(MomentComment comment);
    Task DeleteCommentAsync(MomentComment comment);
    Task<IEnumerable<TourMoment>> GetMomentFeedPagedAsync(int? scheduleId, int currentUserId, int skip, int top);
    Task<List<FootprintDto>> GetUserFootprintsAsync(int userId);
    Task<IEnumerable<TourMoment>> GetUserMomentsAsync(int targetUserId, int currentUserId, bool isFriend);
}