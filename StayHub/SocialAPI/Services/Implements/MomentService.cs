using AutoMapper;
using AutoMapper.QueryableExtensions;
using SocialAPI.DTOs;
using SocialAPI.Helpers;
using SocialAPI.Models;
using SocialAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SocialAPI.Services.Implements;

public class MomentService : IMomentService
{
    private readonly IMomentRepository _momentRepository;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly IMapper _mapper;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IContentModerator _contentModerator;
    private readonly StayHubSocialDbContext _dbContext;
    private readonly IBookingApiClient _bookingApiClient;
    private readonly ITourApiClient _tourApiClient;
    private readonly IAuthApiClient _authApiClient;

    public MomentService(
        IMomentRepository momentRepository,
        ICloudStorageService cloudStorageService,
        IMapper mapper,
        IHttpClientFactory httpClientFactory,
        IFriendshipRepository friendshipRepository,
        IContentModerator contentModerator,
        StayHubSocialDbContext dbContext,
        IBookingApiClient bookingApiClient,
        ITourApiClient tourApiClient,
        IAuthApiClient authApiClient)
    {
        _momentRepository = momentRepository;
        _cloudStorageService = cloudStorageService;
        _mapper = mapper;
        _httpClientFactory = httpClientFactory;
        _friendshipRepository = friendshipRepository;
        _contentModerator = contentModerator;
        _dbContext = dbContext;
        _bookingApiClient = bookingApiClient;
        _tourApiClient = tourApiClient;
        _authApiClient = authApiClient;
    }

    public async Task<MomentResponseDto> CreateMomentAsync(MomentCreateDto dto)
    {
        // Enforce validation rules for posting moments
        if (dto.ScheduleId > 0)
        {
            var metadata = await _tourApiClient.GetScheduleMetadataAsync(dto.ScheduleId, null);
            if (metadata == null)
            {
                throw new ArgumentException("The specified tour schedule was not found.");
            }

            var now = DateTime.UtcNow.Date;
            bool isOngoing = now >= metadata.DepartureDate.Date && now <= metadata.ReturnDate.Date;
            if (!isOngoing)
            {
                throw new ArgumentException("You can only post moments for currently ongoing tours.");
            }
        }
        else if (dto.ScheduleId == 0)
        {
            if (dto.Privacy != null && dto.Privacy.Equals("Tour", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Personal moments cannot be shared with tour privacy.");
            }
        }

        string imageUrl = await _cloudStorageService.UploadImageAsync(dto.Image, "stayhub/social/moments");

        string status = "Approved";
        if (!string.IsNullOrEmpty(dto.Caption))
        {
            status = await _contentModerator.ModerateTextAsync(dto.Caption);
            if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The post caption contains inappropriate words that violate community guidelines.");
            }
        }

        var moment = new TourMoment
        {
            ScheduleId = dto.ScheduleId,
            UserId = dto.UserId,
            ImageUrl = imageUrl,
            Caption = dto.Caption,
            Lat = dto.Lat,
            Lng = dto.Lng,
            Privacy = dto.Privacy,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        var createdMoment = await _momentRepository.CreateMomentAsync(moment);
        return _mapper.Map<MomentResponseDto>(createdMoment);
    }

    public async Task<IEnumerable<MomentResponseDto>> GetMomentsByScheduleIdAsync(int scheduleId, int currentUserId)
    {
        var moments = await _momentRepository.GetMomentsByScheduleIdAsync(scheduleId, currentUserId);
        return _mapper.Map<IEnumerable<MomentResponseDto>>(moments);
    }

    public IQueryable<MomentResponseDto> GetMomentsAsQueryable(int scheduleId)
    {
        return _momentRepository.GetMomentsAsQueryable()
            .Where(m => m.ScheduleId == scheduleId)
            .ProjectTo<MomentResponseDto>(_mapper.ConfigurationProvider);
    }

    public async Task ToggleReactionAsync(int momentId, ReactionRequestDto dto)
    {
        var moment = await _momentRepository.GetMomentByIdAsync(momentId);
        if (moment == null) throw new KeyNotFoundException("Moment not found.");

        var existingReaction = await _momentRepository.GetReactionAsync(momentId, dto.UserId);
        if (existingReaction != null)
        {
            if (existingReaction.IsLike == dto.IsLike)
            {
                await _momentRepository.RemoveReactionAsync(existingReaction);
            }
            else
            {
                existingReaction.IsLike = dto.IsLike;
                await _momentRepository.UpdateReactionAsync(existingReaction);
            }
        }
        else
        {
            var reaction = new MomentReaction { MomentId = momentId, UserId = dto.UserId, IsLike = dto.IsLike };
            await _momentRepository.AddReactionAsync(reaction);

            // Gửi thông báo cho chủ bài viết (chỉ khi like bài của người khác)
            if (moment.UserId != dto.UserId && dto.IsLike == true)
            {
                await SendMomentNotificationAsync(
                    recipientUserId: moment.UserId,
                    actorUserId: dto.UserId,
                    title: "Tim moi tren bai viet cua ban",
                    contentTemplate: "da bam tim bai viet cua ban.",
                    notifType: "moment_like"
                );
            }
        }
    }

    public async Task<CommentResponseDto> AddCommentAsync(int momentId, CommentRequestDto dto)
    {
        var moment = await _momentRepository.GetMomentByIdAsync(momentId);
        if (moment == null) throw new KeyNotFoundException("Moment not found.");

        string status = "Approved";
        if (!string.IsNullOrEmpty(dto.Comment))
        {
            status = await _contentModerator.ModerateTextAsync(dto.Comment);
            if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The comment contains inappropriate words that violate community guidelines.");
            }
        }

        var comment = new MomentComment 
        { 
            MomentId = momentId, 
            UserId = dto.UserId, 
            Comment = dto.Comment, 
            Status = status, 
            Timestamp = DateTime.UtcNow 
        };
        var createdComment = await _momentRepository.AddCommentAsync(comment);

        var result = _mapper.Map<CommentResponseDto>(createdComment);

        // Enrich thong tin nguoi binh luan
        var profiles = await FetchUserProfilesAsync(new List<int> { dto.UserId });
        if (profiles.TryGetValue(dto.UserId, out var p))
        {
            result.UserName = p.FullName;
            result.AvatarUrl = p.AvatarUrl;
        }

        // Gui thong bao cho chu bai viet (chi khi comment bai cua nguoi khac)
        if (moment.UserId != dto.UserId)
        {
            await SendMomentNotificationAsync(
                recipientUserId: moment.UserId,
                actorUserId: dto.UserId,
                title: "Binh luan moi tren bai viet cua ban",
                contentTemplate: "da binh luan tren bai viet cua ban.",
                notifType: "moment_comment"
            );
        }

        return result;
    }

    public async Task<CommentResponseDto> UpdateCommentAsync(int commentId, CommentRequestDto dto)
    {
        var comment = await _momentRepository.GetCommentByIdAsync(commentId);
        if (comment == null) throw new KeyNotFoundException("Comment not found.");
        if (comment.UserId != dto.UserId) throw new UnauthorizedAccessException("You do not have permission to update this comment.");

        string status = "Approved";
        if (!string.IsNullOrEmpty(dto.Comment))
        {
            status = await _contentModerator.ModerateTextAsync(dto.Comment);
            if (status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The comment contains inappropriate words that violate community guidelines.");
            }
        }

        comment.Comment = dto.Comment;
        comment.Status = status;
        await _momentRepository.UpdateCommentAsync(comment);

        return _mapper.Map<CommentResponseDto>(comment);
    }

    public async Task DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _momentRepository.GetCommentByIdAsync(commentId);
        if (comment == null) throw new KeyNotFoundException("Comment not found.");

        if (comment.UserId != userId) throw new UnauthorizedAccessException("You do not have the right to delete this comment.");

        await _momentRepository.DeleteCommentAsync(comment);
    }

    public async Task DeleteMomentAsync(int momentId, int userId)
    {
        var moment = await _momentRepository.GetMomentByIdAsync(momentId);
        if (moment == null) throw new KeyNotFoundException("Moment not found.");

        if (moment.UserId != userId) throw new UnauthorizedAccessException("You do not have the right to delete this moment.");

        await _momentRepository.DeleteMomentAsync(moment);
    }

    public async Task<IEnumerable<MomentResponseDto>> GetMomentFeedWithUsersAsync(int? scheduleId, int currentUserId, string? bearerToken, int skip, int top)
    {
        var rawMoments = (await _momentRepository.GetMomentFeedPagedAsync(scheduleId, currentUserId, skip, top)).ToList();
        
        var moments = new List<TourMoment>();
        foreach (var m in rawMoments)
        {
            if (m.Privacy.Equals("Tour", StringComparison.OrdinalIgnoreCase) && m.UserId != currentUserId)
            {
                bool isMember = await CheckIsTourMemberAsync(m.ScheduleId, currentUserId, bearerToken);
                if (!isMember)
                {
                    continue;
                }
            }
            moments.Add(m);
        }

        var dtos = _mapper.Map<List<MomentResponseDto>>(moments);

        if (!dtos.Any()) return dtos;

        // ✅ 1) Xác định moment nào đã được currentUser thả tim (lấy trực tiếp từ entity).
        var likedMomentIds = moments
            .Where(m => m.MomentReactions.Any(r => r.UserId == currentUserId && r.IsLike == true))
            .Select(m => m.Id)
            .ToHashSet();

        // ✅ 2) Gom userId của TÁC GIẢ moment LẪN người BÌNH LUẬN -> fetch 1 lần.
        var userIds = dtos.Select(d => d.UserId)
            .Concat(dtos.SelectMany(d => d.Comments).Select(c => c.UserId))
            .Distinct()
            .ToList();

        var userProfiles = await FetchUserProfilesAsync(userIds);

        foreach (var dto in dtos)
        {
            // Trạng thái like của tôi + số đếm (đã map sẵn ReactionCount/TotalLikes)
            dto.IsLikedByMe = likedMomentIds.Contains(dto.Id);

            // Thông tin tác giả moment
            if (dto.User != null)
            {
                if (userProfiles.TryGetValue(dto.UserId, out var author))
                {
                    dto.User.FullName = author.FullName ?? "Anonymous user";
                    dto.User.AvatarUrl = author.AvatarUrl;
                }
                else
                {
                    dto.User.FullName = "Anonymous user";
                }
            }

            // ✅ 3) Thông tin từng người bình luận
            foreach (var c in dto.Comments)
            {
                if (userProfiles.TryGetValue(c.UserId, out var cu))
                {
                    c.UserName = cu.FullName ?? "Anonymous user";
                    c.AvatarUrl = cu.AvatarUrl;
                }
                else
                {
                    c.UserName = "Anonymous user";
                }
            }
        }

        return dtos;
    }

    private async Task<bool> CheckIsTourMemberAsync(int scheduleId, int userId, string? bearerToken)
    {
        if (scheduleId <= 0) return false;
        try
        {
            var metadata = await _tourApiClient.GetScheduleMetadataAsync(scheduleId, bearerToken);
            if (metadata != null)
            {
                if (metadata.TourCreatedBy == userId || (metadata.StaffIds != null && metadata.StaffIds.Contains(userId)))
                {
                    return true;
                }
            }

            var bookedCustomerIds = await _bookingApiClient.GetCustomerIdsByScheduleAsync(scheduleId);
            if (bookedCustomerIds != null && bookedCustomerIds.Contains(userId))
            {
                return true;
            }
        }
        catch { }
        return false;
    }

    // ✅ Helper dùng chung: batch fetch user profiles (an toàn nếu user service lỗi).
    private async Task<Dictionary<int, UserProfileShortDto>> FetchUserProfilesAsync(List<int> userIds)
    {
        if (userIds == null || userIds.Count == 0) return new Dictionary<int, UserProfileShortDto>();
        try
        {
            return await _authApiClient.GetUserProfilesAsync(userIds);
        }
        catch
        {
            // Nuốt lỗi để feed vẫn load được khi user service không khả dụng.
            return new Dictionary<int, UserProfileShortDto>();
        }
    }

    public async Task<IEnumerable<FootprintDto>> GetMyFootprintsAsync(int userId)
    {
        return await _momentRepository.GetUserFootprintsAsync(userId);
    }

    public async Task<IEnumerable<UserMomentResponseDto>> GetUserMomentsAsync(int targetUserId, int currentUserId)
    {
        bool isFriend = false;
        if (targetUserId != currentUserId)
        {
            isFriend = await _friendshipRepository.CheckAreFriendsAsync(currentUserId, targetUserId);
        }

        var moments = await _momentRepository.GetUserMomentsAsync(targetUserId, currentUserId, isFriend);
        return _mapper.Map<IEnumerable<UserMomentResponseDto>>(moments);
    }

    public async Task ReportContentAsync(int reporterId, string contentType, int targetId, string reason, string? details)
    {
        if (contentType.Equals("Moment", StringComparison.OrdinalIgnoreCase))
        {
            var moment = await _dbContext.TourMoments.FindAsync(targetId);
            if (moment == null) throw new KeyNotFoundException("Moment not found.");
        }
        else if (contentType.Equals("Comment", StringComparison.OrdinalIgnoreCase))
        {
            var comment = await _dbContext.MomentComments.FindAsync(targetId);
            if (comment == null) throw new KeyNotFoundException("Comment not found.");
        }
        else
        {
            throw new ArgumentException("Invalid content type. Must be 'Moment' or 'Comment'.");
        }

        var report = new ContentReport
        {
            ReporterId = reporterId,
            ContentType = contentType,
            TargetId = targetId,
            Reason = reason,
            Details = details,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.ContentReports.AddAsync(report);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ContentReport>> GetPendingReportsAsync()
    {
        var reports = await _dbContext.ContentReports
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        if (reports.Any())
        {
            var reporterIds = reports.Select(r => r.ReporterId).Distinct().ToList();
            var profiles = await FetchUserProfilesAsync(reporterIds);

            foreach (var report in reports)
            {
                if (profiles.TryGetValue(report.ReporterId, out var profile))
                {
                    report.ReporterName = profile.FullName;
                    report.ReporterEmail = profile.Email;
                }
                else
                {
                    report.ReporterName = $"Người dùng #{report.ReporterId}";
                }

                if (report.ContentType.Equals("Moment", StringComparison.OrdinalIgnoreCase))
                {
                    var moment = await _dbContext.TourMoments.FindAsync(report.TargetId);
                    if (moment != null)
                    {
                        report.ContentText = moment.Caption;
                        report.ContentImageUrl = moment.ImageUrl;
                    }
                }
                else if (report.ContentType.Equals("Comment", StringComparison.OrdinalIgnoreCase))
                {
                    var comment = await _dbContext.MomentComments.FindAsync(report.TargetId);
                    if (comment != null)
                    {
                        report.ContentText = comment.Comment;
                    }
                }
            }
        }

        return reports;
    }

    public async Task ResolveReportAsync(int reportId, string action, int resolvedBy)
    {
        var report = await _dbContext.ContentReports.FindAsync(reportId);
        if (report == null) throw new KeyNotFoundException("Report not found.");

        if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
        {
            if (report.ContentType.Equals("Moment", StringComparison.OrdinalIgnoreCase))
            {
                var moment = await _dbContext.TourMoments.FindAsync(report.TargetId);
                if (moment != null)
                {
                    moment.Status = "Rejected";
                    _dbContext.TourMoments.Update(moment);
                }
            }
            else if (report.ContentType.Equals("Comment", StringComparison.OrdinalIgnoreCase))
            {
                var comment = await _dbContext.MomentComments.FindAsync(report.TargetId);
                if (comment != null)
                {
                    comment.Status = "Rejected";
                    _dbContext.MomentComments.Update(comment);
                }
            }
            report.Status = "Resolved";
        }
        else if (action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
        {
            if (report.ContentType.Equals("Moment", StringComparison.OrdinalIgnoreCase))
            {
                var moment = await _dbContext.TourMoments.FindAsync(report.TargetId);
                if (moment != null)
                {
                    moment.Status = "Approved";
                    _dbContext.TourMoments.Update(moment);
                }
            }
            else if (report.ContentType.Equals("Comment", StringComparison.OrdinalIgnoreCase))
            {
                var comment = await _dbContext.MomentComments.FindAsync(report.TargetId);
                if (comment != null)
                {
                    comment.Status = "Approved";
                    _dbContext.MomentComments.Update(comment);
                }
            }
            report.Status = "Resolved";
        }
        else if (action.Equals("Dismiss", StringComparison.OrdinalIgnoreCase))
        {
            if (report.ContentType.Equals("Moment", StringComparison.OrdinalIgnoreCase))
            {
                var moment = await _dbContext.TourMoments.FindAsync(report.TargetId);
                if (moment != null)
                {
                    moment.Status = "Approved";
                    _dbContext.TourMoments.Update(moment);
                }
            }
            else if (report.ContentType.Equals("Comment", StringComparison.OrdinalIgnoreCase))
            {
                var comment = await _dbContext.MomentComments.FindAsync(report.TargetId);
                if (comment != null)
                {
                    comment.Status = "Approved";
                    _dbContext.MomentComments.Update(comment);
                }
            }
            report.Status = "Dismissed";
        }
        else
        {
            throw new ArgumentException("Invalid action. Must be 'Approve', 'Reject', or 'Dismiss'.");
        }

        report.ResolvedBy = resolvedBy;
        report.ResolvedAt = DateTime.UtcNow;

        _dbContext.ContentReports.Update(report);

        // Tự động duyệt/đóng toàn bộ báo cáo Pending trùng lặp đối với cùng một đối tượng (Moment/Comment)
        var duplicateReports = await _dbContext.ContentReports
            .Where(r => r.Id != reportId 
                        && r.ContentType == report.ContentType 
                        && r.TargetId == report.TargetId 
                        && r.Status == "Pending")
            .ToListAsync();

        foreach (var dupReport in duplicateReports)
        {
            dupReport.Status = action.Equals("Dismiss", StringComparison.OrdinalIgnoreCase) ? "Dismissed" : "Resolved";
            dupReport.ResolvedBy = resolvedBy;
            dupReport.ResolvedAt = DateTime.UtcNow;
            _dbContext.ContentReports.Update(dupReport);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<MomentResponseDto?> GetMomentByIdAsync(int momentId, int currentUserId)
    {
        var moment = await _momentRepository.GetMomentByIdAsync(momentId);
        if (moment == null) return null;

        // Xác thực quyền riêng tư dựa trên loại Privacy
        if (moment.Privacy.Equals("Private", StringComparison.OrdinalIgnoreCase) && moment.UserId != currentUserId)
        {
            return null; 
        }
        if (moment.Privacy.Equals("Friend", StringComparison.OrdinalIgnoreCase) && moment.UserId != currentUserId)
        {
            var isFriend = await _friendshipRepository.CheckAreFriendsAsync(moment.UserId, currentUserId);
            if (!isFriend)
            {
                return null;
            }
        }

        var dto = _mapper.Map<MomentResponseDto>(moment);
        
        // Đánh dấu IsLikedByMe
        dto.IsLikedByMe = moment.MomentReactions.Any(r => r.UserId == currentUserId && r.IsLike == true);

        // Gom danh sách userIds để batch fetch thông tin
        var userIds = new List<int> { dto.UserId };
        userIds.AddRange(dto.Comments.Select(c => c.UserId));
        userIds = userIds.Distinct().ToList();

        var userProfiles = await FetchUserProfilesAsync(userIds);

        // Gán thông tin tác giả moment
        if (userProfiles.TryGetValue(dto.UserId, out var author))
        {
            dto.User = new MomentUserDto
            {
                Id = author.Id,
                FullName = author.FullName ?? "Anonymous user",
                AvatarUrl = author.AvatarUrl
            };
        }
        else if (dto.User != null)
        {
            dto.User.FullName = "Anonymous user";
        }

        // Gán thông tin người bình luận
        foreach (var c in dto.Comments)
        {
            if (userProfiles.TryGetValue(c.UserId, out var cu))
            {
                c.UserName = cu.FullName ?? "Anonymous user";
                c.AvatarUrl = cu.AvatarUrl;
            }
            else
            {
                c.UserName = "Anonymous user";
            }
        }

        return dto;
    }

    /// <summary>
    /// Gui thong bao den SystemAPI khi nguoi dung like/comment bai viet cua nguoi khac.
    /// </summary>
    private async Task SendMomentNotificationAsync(
        int recipientUserId,
        int actorUserId,
        string title,
        string contentTemplate,
        string notifType)
    {
        try
        {
            // Lay ten nguoi thuc hien hanh dong
            string actorName = $"Nguoi dung #{actorUserId}";
            try
            {
                var profiles = await _authApiClient.GetUserProfilesAsync(new List<int> { actorUserId });
                if (profiles != null && profiles.TryGetValue(actorUserId, out var profile) && !string.IsNullOrWhiteSpace(profile.FullName))
                {
                    actorName = profile.FullName;
                }
            }
            catch { /* Fallback to generic name */ }

            var internalKey = "stayhub-internal-2025-xK9mP";
            var client = _httpClientFactory.CreateClient("SystemApiClient");
            var payload = new
            {
                UserId = recipientUserId,
                Title = title,
                Content = $"{actorName} {contentTemplate}"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/internal/send")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Internal-Key", internalKey);
            await client.SendAsync(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MomentService] Failed to send {notifType} notification: {ex.Message}");
        }
    }
}
