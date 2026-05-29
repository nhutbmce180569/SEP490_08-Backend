using SocialAPI.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialAPI.Services;

public interface IMomentService
{
    // UseCase 27: Share Moment || Moment Management - Create Moment.
    Task<MomentResponseDto> CreateMomentAsync(MomentCreateDto dto);

    //UseCase 28: View Moments || Moment Management - Get Moments for Schedule. 
    Task<IEnumerable<MomentResponseDto>> GetMomentsByScheduleIdAsync(int scheduleId, int currentUserId);
    //UseCase 28: View Moments || Moment Management - Get Moments for Schedule.
    IQueryable<MomentResponseDto> GetMomentsAsQueryable(int scheduleId);
    //UseCase 29: React or Unreact to Moments || Moment Management - Toggle Reaction.
    Task ToggleReactionAsync(int momentId, ReactionRequestDto dto);
    //UseCase 30: Comment on Moments || Moment Management - Add Comment.
    Task<CommentResponseDto> AddCommentAsync(int momentId, CommentRequestDto dto);
    Task<CommentResponseDto> UpdateCommentAsync(int commentId, CommentRequestDto dto);
    Task DeleteCommentAsync(int commentId, int userId);
    Task DeleteMomentAsync(int momentId, int userId);
    Task<IEnumerable<MomentResponseDto>> GetMomentFeedWithUsersAsync(int scheduleId, int currentUserId, int skip, int top);
    Task<IEnumerable<FootprintDto>> GetMyFootprintsAsync(int userId);
}